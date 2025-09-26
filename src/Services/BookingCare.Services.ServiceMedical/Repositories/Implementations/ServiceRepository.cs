using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Data;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.ServiceMedical.Repositories.Implementations
{
    public class ServiceRepository : IServiceRepository
    {
        private readonly ServiceMedicalDbContext _context;

        public ServiceRepository(ServiceMedicalDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceEntity> CreateAsync(ServiceEntity entity)
        {
            _context.Services.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ServiceEntity?> GetByIdAsync(Guid id)
        {
            return await _context.Services
                .Include(s => s.ServiceCategory)
                .Include(s => s.Schedules)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<ServiceEntity> UpdateAsync(ServiceEntity entity)
        {
            _context.Services.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.Services.FindAsync(id);
            if (entity == null) return false;

            _context.Services.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ServiceEntity>> GetAllAsync()
        {
            return await _context.Services
                .Include(s => s.ServiceCategory)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceEntity>> GetActiveServicesAsync()
        {
            return await _context.Services
                .Where(s => s.Status == StatusConstants.Active)
                .Include(s => s.ServiceCategory)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceEntity>> GetServicesByCategoryAsync(Guid categoryId)
        {
            return await _context.Services
                .Where(s => s.ServiceCategoryId == categoryId && s.Status == StatusConstants.Active)
                .Include(s => s.ServiceCategory)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceEntity>> GetServicesByHospitalAsync(Guid hospitalId)
        {
            return await _context.Services
                .Where(s => s.HospitalId == hospitalId && s.Status == StatusConstants.Active)
                .Include(s => s.ServiceCategory)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<(List<ServiceEntity> Services, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? searchTerm = null, string? status = null,
            Guid? hospitalId = null, Guid? categoryId = null, decimal? minPrice = null, decimal? maxPrice = null)
        {
            var query = _context.Services
                .Include(s => s.ServiceCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(s => s.Name.Contains(searchTerm) ||
                                        (s.Description != null && s.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(s => s.Status == status);
            }

            if (hospitalId.HasValue)
            {
                query = query.Where(s => s.HospitalId == hospitalId.Value);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(s => s.ServiceCategoryId == categoryId.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(s => s.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(s => s.Price <= maxPrice.Value);
            }

            var totalCount = await query.CountAsync();
            var services = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (services, totalCount);
        }

        public async Task<List<Guid>> GetHospitalIdsByCategoryAsync(Guid categoryId)
        {
            return await _context.Services
                .Where(s => s.ServiceCategoryId == categoryId && s.Status == StatusConstants.Active)
                .Select(s => s.HospitalId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<ServiceEntity>> GetServicesWithCategoryAsync()
        {
            return await _context.Services
                .Include(s => s.ServiceCategory)
                .Where(s => s.Status == StatusConstants.Active)
                .OrderBy(s => s.ServiceCategory!.Name)
                .ThenBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.Services.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> ServiceExistsForHospitalAsync(Guid serviceId, Guid hospitalId)
        {
            return await _context.Services.AnyAsync(s => s.Id == serviceId && s.HospitalId == hospitalId);
        }

        public IQueryable<ServiceEntity> GetQueryable()
        {
            return _context.Services.AsQueryable();
        }
    }
}
