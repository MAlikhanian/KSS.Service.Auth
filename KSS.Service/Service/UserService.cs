using AutoMapper;
using KSS.Dto;
using KSS.Entity;
using KSS.Helper;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using System.Linq.Expressions;

namespace KSS.Service.Service
{
    public class UserService : BaseService<User, UserDto, UserDto, UserDto>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonApiClient _personApiClient;

        public UserService(IMapper mapper, IUserRepository repository, IPersonApiClient personApiClient) : base(mapper, repository)
        {
            _userRepository = repository;
            _personApiClient = personApiClient;
        }

        public async Task<UserDto> RegisterAsync(RegisterRequestDto request, Guid? tenantCompanyId = null)
        {
            // 1. Check username not exists
            var existingUserByUsername = await _userRepository.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (existingUserByUsername != null)
                throw new BusinessRuleException("DUPLICATE_USERNAME");

            // 2. Check email not exists
            var existingUserByEmail = await _userRepository.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (existingUserByEmail != null)
                throw new BusinessRuleException("DUPLICATE_EMAIL");

            // 3. Check phone not exists (if provided)
            if (!string.IsNullOrEmpty(request.Phone))
            {
                var existingUserByPhone = await _userRepository.SingleOrDefaultAsync(u => u.Phone == request.Phone);
                if (existingUserByPhone != null)
                    throw new BusinessRuleException("DUPLICATE_PHONE");
            }

            // 4. Call Person API to create person and get PersonId.
            // Leave SexId / DateOfBirth / Birth*Id at DTO defaults — the user
            // fills them in from the profile page. PreferredLanguageId
            // defaults to 12 (Persian); pass it explicitly if the caller wants
            // to override.
            var createPersonRequest = new CreatePersonRequestDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                NationalId = request.Username,
                BirthCountryId = request.CountryId ?? 1,
            };

            // Tenant membership: when the signup arrived on a tenant's hostname, the
            // company travels with the Person call as X-Company-Id and Person's own
            // auto-assign creates the CompanyPerson row. Unknown host => null => no
            // membership, exactly as before.
            var createdPerson = await _personApiClient.CreatePersonAsync(createPersonRequest, tenantCompanyId);

            // 5. Insert user with PersonId — Id, CreatedBy, CreatedAt are set by ApplyEntityDefaults
            var user = new User
            {
                PersonId = createdPerson.Id,
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                CountryId = request.CountryId,
                PasswordHash = Authentication.PasswordHash(request.Password),
                IsActive = true,
                IsEmailVerified = false,
                IsPhoneVerified = false,
                FailedLoginAttempts = 0
            };

            await _userRepository.AddAsync(user);

            // 6. Assign default "User" role (all permissions except Admin)
            await _userRepository.AssignDefaultRoleAsync(user.Id);

            return _mapper.Map<UserDto>(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string jwtSecret)
        {
            // Find user by username
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException($"Account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive");
            }

            // Verify password
            if (!Authentication.PasswordVerify(user.PasswordHash, request.Password))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;
                
                // Lock account after 5 failed attempts for 30 minutes
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                }

                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;

            // Generate refresh token
            var refreshToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7); // 7 days expiry

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Load user roles and permissions for JWT claims
            var roles = await _userRepository.GetUserRolesAsync(user.Id);
            var roleIds = await _userRepository.GetUserRoleIdsAsync(user.Id);
            var permissions = await _userRepository.GetUserPermissionsAsync(user.Id);

            // Generate JWT token with roles, role Ids, permissions, and PersonId.
            // RoleIds power the Person service's RoleAccess lookups.
            var token = Authentication.JwtTokenGenerate(jwtSecret, user.Username, roles, permissions, user.PersonId, roleIds, 180);

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                TokenExpires = DateTime.UtcNow.AddMinutes(180),
                User = _mapper.Map<UserDto>(user),
                Roles = roles,
                Permissions = permissions
            };
        }

        public async Task<UserDto?> GetByUsernameAsync(string username)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Username == username);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto?> GetByPersonIdAsync(Guid personId)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.PersonId == personId);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<IDictionary<Guid, Guid>> MapPersonsToUsersAsync(IEnumerable<Guid> personIds)
        {
            // Materialize input once; tolerate duplicates and Guid.Empty.
            var ids = personIds?
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return new Dictionary<Guid, Guid>();

            var users = await _userRepository.ToListAsync(u => u.PersonId.HasValue && ids.Contains(u.PersonId.Value));
            return users.ToDictionary(u => u.PersonId!.Value, u => u.Id);
        }

        // ─── Security management (powers the /person/security page) ───

        public async Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
        {
            ValidatePassword(newPassword);

            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            if (!Authentication.PasswordVerify(user.PasswordHash, currentPassword))
                throw new BusinessRuleException("Current password is incorrect");

            user.PasswordHash = Authentication.PasswordHash(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task AdminResetPasswordAsync(Guid userId, string newPassword)
        {
            ValidatePassword(newPassword);

            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.PasswordHash = Authentication.PasswordHash(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetExpires = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task LockAsync(Guid userId, int lockMinutes)
        {
            if (lockMinutes <= 0)
                throw new BusinessRuleException("Lock duration must be positive");

            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.LockedUntil = DateTime.UtcNow.AddMinutes(lockMinutes);
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task UnlockAsync(Guid userId)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.LockedUntil = null;
            user.FailedLoginAttempts = 0;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task MarkEmailVerifiedAsync(Guid userId)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.IsEmailVerified = true;
            user.EmailVerifiedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task MarkPhoneVerifiedAsync(Guid userId)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.IsPhoneVerified = true;
            user.PhoneVerifiedAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task SetActiveAsync(Guid userId, bool isActive)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.IsActive = isActive;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task RevokeSessionsAsync(Guid userId)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Id == userId)
                       ?? throw new BusinessRuleException("User not found");

            user.RefreshToken = null;
            user.RefreshTokenExpires = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
                throw new BusinessRuleException("Password must be at least 8 characters");
        }
    }
}
