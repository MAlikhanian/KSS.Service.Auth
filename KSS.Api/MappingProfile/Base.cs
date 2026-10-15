using AutoMapper;
using KSS.Entity;
using KSS.Dto;

namespace KSS.Api.MappingProfile
{
    public class BaseMappingProfile : Profile
    {
        public BaseMappingProfile()
        {
            CreateMap<User, UserDto>().ReverseMap();

            // Authentication DTOs - no reverse mapping needed as they're request/response only
            CreateMap<RegisterRequestDto, User>();
            CreateMap<LoginRequestDto, User>().ReverseMap();

            // Role and Permission mappings
            CreateMap<Role, RoleDto>().ReverseMap();
            CreateMap<Permission, PermissionDto>().ReverseMap();
        }
    }
}