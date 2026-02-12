using System.Linq.Expressions;
using AutoMapper;
using KSS.Helper;
using KSS.Helper.Model;
using KSS.Repository.IRepository;
using KSS.Service.IService;

namespace KSS.Service.Service
{
    public class BaseService<T, TViewDto, TAddDto, TUpdateDto> : IBaseService<T, TViewDto, TAddDto, TUpdateDto>
        where T : class
        where TViewDto : class
        where TAddDto : class
        where TUpdateDto : class
    {
        protected readonly IMapper _mapper;
        private readonly IBaseRepository<T> _repository;

        protected byte TableId { get; set; }

        public BaseService(IMapper mapper, IBaseRepository<T> repository)
        {
            _mapper = mapper;
            _repository = repository;
        }

        #region Find

        public virtual Task<T> FindAsync(Filter id) => _repository.FindAsync(id);

        public virtual T Find(object id) => _repository.Find(id);

        #endregion

        #region Single

        public async Task<T> SingleAsync() => await _repository.SingleAsync();
        public async Task<T> SingleAsync(Expression<Func<T, bool>> filter) => await _repository.SingleAsync(filter);
        public async Task<T> SingleAsync(IQueryable<T> queryable) => await _repository.SingleAsync(queryable);
        public virtual Task<T> SingleAsync(T filter) => _repository.SingleAsync(filter);

        public T Single() => _repository.Single();
        public T Single(Expression<Func<T, bool>> filter) => _repository.Single(filter);
        public T Single(IQueryable<T> queryable) => _repository.Single(queryable);

        public Task<T> SingleUnawaited() => _repository.SingleUnawaited();
        public Task<T> SingleUnawaited(Expression<Func<T, bool>> filter) => _repository.SingleUnawaited(filter);
        public Task<T> SingleUnawaited(IQueryable<T> queryable) => _repository.SingleUnawaited(queryable);


        public Task<T> SingleOrDefaultAsync() => _repository.SingleOrDefaultAsync();
        public Task<T> SingleOrDefaultAsync(Expression<Func<T, bool>> filter) => _repository.SingleOrDefaultAsync(filter);
        public Task<T> SingleOrDefaultAsync(IQueryable<T> queryable) => _repository.SingleOrDefaultAsync(queryable);

        public T SingleOrDefault() => _repository.SingleOrDefault();
        public T SingleOrDefault(Expression<Func<T, bool>> filter) => _repository.SingleOrDefault(filter);
        public T SingleOrDefault(IQueryable<T> queryable) => _repository.SingleOrDefault(queryable);

        public Task<T> SingleOrDefaultUnawaited() => _repository.SingleOrDefaultUnawaited();
        public Task<T> SingleOrDefaultUnawaited(Expression<Func<T, bool>> filter) => _repository.SingleOrDefaultUnawaited(filter);
        public Task<T> SingleOrDefaultUnawaited(IQueryable<T> queryable) => _repository.SingleOrDefaultUnawaited(queryable);

        #endregion

        #region First

        public Task<T> FirstAsync() => _repository.FirstAsync();
        public Task<T> FirstAsync(Expression<Func<T, bool>> filter) => _repository.FirstAsync(filter);
        public Task<T> FirstAsync(IQueryable<T> queryable) => _repository.FirstAsync(queryable);

        public T First() => _repository.First();
        public T First(Expression<Func<T, bool>> filter) => _repository.First(filter);
        public T First(IQueryable<T> queryable) => _repository.First(queryable);

        public Task<T> FirstUnawaited() => _repository.FirstUnawaited();
        public Task<T> FirstUnawaited(Expression<Func<T, bool>> filter) => _repository.FirstUnawaited(filter);
        public Task<T> FirstUnawaited(IQueryable<T> queryable) => _repository.FirstUnawaited(queryable);


        public Task<T> FirstOrDefaultAsync() => _repository.FirstOrDefaultAsync();
        public Task<T> FirstOrDefaultAsync(Expression<Func<T, bool>> filter) => _repository.FirstOrDefaultAsync(filter);
        public Task<T> FirstOrDefaultAsync(IQueryable<T> queryable) => _repository.FirstOrDefaultAsync(queryable);

        public T FirstOrDefault() => _repository.FirstOrDefault();
        public T FirstOrDefault(Expression<Func<T, bool>> filter) => _repository.FirstOrDefault(filter);
        public T FirstOrDefault(IQueryable<T> queryable) => _repository.FirstOrDefault(queryable);

        public Task<T> FirstOrDefaultUnawaited() => _repository.FirstOrDefaultUnawaited();
        public Task<T> FirstOrDefaultUnawaited(Expression<Func<T, bool>> filter) => _repository.FirstOrDefaultUnawaited(filter);
        public Task<T> FirstOrDefaultUnawaited(IQueryable<T> queryable) => _repository.FirstOrDefaultUnawaited(queryable);

        #endregion

        #region ToList

        public virtual Task<IEnumerable<T>> ToListAsync() => _repository.ToListAsync();
        public virtual Task<IEnumerable<T>> ToListAsync(Expression<Func<T, bool>> filter) => _repository.ToListAsync(filter);
        public virtual Task<IEnumerable<T>> ToListAsync(IQueryable<T> queryable) => _repository.ToListAsync(queryable);
        public virtual Task<IEnumerable<T>> ToListAsync(T filter) => _repository.ToListAsync(filter);
        public virtual Task<IEnumerable<T>> ToListAsync(Filter filter) => _repository.ToListAsync(filter);

        public virtual IEnumerable<T> ToList() => _repository.ToList();
        public virtual IEnumerable<T> ToList(Expression<Func<T, bool>> filter) => _repository.ToList(filter);
        public virtual IEnumerable<T> ToList(IQueryable<T> queryable) => _repository.ToList(queryable);

        public virtual Task<List<T>> ToListUnawaited() => _repository.ToListUnawaited();
        public virtual Task<List<T>> ToListUnawaited(Expression<Func<T, bool>> filter) => _repository.ToListUnawaited(filter);
        public virtual Task<List<T>> ToListUnawaited(IQueryable<T> queryable) => _repository.ToListUnawaited(queryable);

        #endregion

        #region Count

        public Task<int> CountAsync() => _repository.CountAsync();
        public Task<int> CountAsync(Expression<Func<T, bool>> filter) => _repository.CountAsync(filter);
        public Task<int> CountAsync(IQueryable<T> queryable) => _repository.CountAsync(queryable);

        public int Count() => _repository.Count();
        public int Count(Expression<Func<T, bool>> filter) => _repository.Count(filter);
        public int Count(IQueryable<T> queryable) => _repository.Count(queryable);

        public Task<int> CountUnawaited() => _repository.CountUnawaited();
        public Task<int> CountUnawaited(Expression<Func<T, bool>> filter) => _repository.CountUnawaited(filter);
        public Task<int> CountUnawaited(IQueryable<T> queryable) => _repository.CountUnawaited(queryable);

        #endregion

        #region Add

        public virtual Task AddAsync(T item, bool saveChanges = true) => _repository.AddAsync(item, saveChanges);
        public virtual Task AddDtoAsync(TAddDto item, bool saveChanges = true) => _repository.AddAsync(AddDto(item), saveChanges);
        public virtual void AddUnawaitedAsync(T item, bool saveChanges = true) => _repository.AddUnawaitedAsync(item, saveChanges);

        public virtual void Add(T item, bool saveChanges = true) => _repository.Add(item, saveChanges);
        public virtual void AddUnawaited(T item, bool saveChanges = true) => _repository.AddUnawaited(item, saveChanges);


        public virtual Task AddRangeAsync(IEnumerable<T> items, bool saveChanges = true) => _repository.AddRangeAsync(items, saveChanges);
        public virtual void AddRangeUnawaitedAsync(IEnumerable<T> items, bool saveChanges = true) => _repository.AddRangeUnawaitedAsync(items, saveChanges);

        public virtual void AddRange(IEnumerable<T> items, bool saveChanges = true) => _repository.AddRange(items, saveChanges);
        public virtual void AddRangeUnawaited(IEnumerable<T> items, bool saveChanges = true) => _repository.AddRangeUnawaited(items, saveChanges);

        #endregion

        #region Update

        public virtual void Update(T item, bool saveChanges = true) => _repository.Update(item, saveChanges);
        public virtual void UpdateDto(TUpdateDto item, bool saveChanges = true) => _repository.Update(UpdateDto(item), saveChanges);
        public virtual void Update(T item, Expression<Func<T, object>>[] properties, bool saveChanges = true) => _repository.Update(item, properties, saveChanges);
        public virtual void UpdateRange(IEnumerable<T> items, bool saveChanges = true) => _repository.UpdateRange(items, saveChanges);

        #endregion

        #region Remove / Delete

        public virtual void Remove(T item, bool saveChanges = true) => _repository.Remove(item, saveChanges);
        public virtual void RemoveRange(IEnumerable<T> items, bool saveChanges = true) => _repository.RemoveRange(items, saveChanges);

        public virtual async Task ExecuteDeleteAsync() => await _repository.ExecuteDeleteAsync();
        public virtual async Task ExecuteDeleteAsync(Expression<Func<T, bool>> filter) => await _repository.ExecuteDeleteAsync(filter);
        public virtual async Task ExecuteDeleteAsync(IQueryable<T> queryable) => await _repository.ExecuteDeleteAsync(queryable);

        public virtual async void ExecuteDeleteUnawaitedAsync() => await _repository.ExecuteDeleteAsync();
        public virtual async void ExecuteDeleteUnawaitedAsync(Expression<Func<T, bool>> filter) => await _repository.ExecuteDeleteAsync(filter);
        public virtual async void ExecuteDeleteUnawaitedAsync(IQueryable<T> queryable) => await _repository.ExecuteDeleteAsync(queryable);

        public virtual void ExecuteDelete() => _repository.ExecuteDelete();
        public virtual void ExecuteDelete(Expression<Func<T, bool>> filter) => _repository.ExecuteDelete(filter);
        public virtual void ExecuteDelete(IQueryable<T> queryable) => _repository.ExecuteDelete(queryable);

        public virtual void ExecuteDeleteUnawaited() => _repository.ExecuteDeleteAsync();
        public virtual void ExecuteDeleteUnawaited(Expression<Func<T, bool>> filter) => _repository.ExecuteDeleteAsync(filter);
        public virtual void ExecuteDeleteUnawaited(IQueryable<T> queryable) => _repository.ExecuteDeleteAsync(queryable);

        #endregion

        #region SaveChanges

        public async Task SaveChangesAsync() => await _repository.SaveChangesAsync();
        public async void SaveChangesUnawaitedAsync() => await _repository.SaveChangesAsync();

        public void SaveChanges() => _repository.SaveChanges();
        public void SaveChangesUnawaited() => _repository.SaveChangesAsync();

        #endregion

        public TViewDto Dto(T item) => _mapper.Map<TViewDto>(item);
        public IEnumerable<TViewDto> Dto(IEnumerable<T> items) => _mapper.Map<IEnumerable<TViewDto>>(items);

        public T AddDto(TAddDto item) => _mapper.Map<T>(item);
        public IEnumerable<T> AddDto(IEnumerable<TAddDto> item) => _mapper.Map<IEnumerable<T>>(item);

        public T UpdateDto(TUpdateDto item) => _mapper.Map<T>(item);
        public IEnumerable<T> UpdateDto(IEnumerable<TUpdateDto> item) => _mapper.Map<IEnumerable<T>>(item);

        //public async Task<Pagination<D>> Pagination<D>(IEnumerable<D> items, int pageNumber, int pageSize, RequestOption requestOption)
        //{
        //    int recordCount = await CountAsync(requestOption);

        //    return new Pagination<D>(items, recordCount, pageNumber, pageSize);
        //}

        public IEnumerable<D> PaginationDto<D>(IEnumerable<T> items) => _mapper.Map<IEnumerable<D>>(items);
    }
}