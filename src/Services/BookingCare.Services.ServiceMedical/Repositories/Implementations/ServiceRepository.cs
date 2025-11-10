using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Data;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Models.DTOs.Requests;
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

            // Soft delete: Only change status to INACTIVE instead of hard delete
            entity.Status = StatusConstants.Inactive;
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

        public async Task<(List<ServiceEntity> Services, int TotalCount)> GetPagedAsync(ServiceQueryRequest request)
        {
            var query = _context.Services
                .Include(s => s.ServiceCategory)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                query = query.Where(s => s.Name.Contains(request.SearchTerm) ||
                                        (s.Description != null && s.Description.Contains(request.SearchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(request.Status))
            {
                query = query.Where(s => s.Status == request.Status);
            }

            if (request.HospitalId.HasValue)
            {
                query = query.Where(s => s.HospitalId == request.HospitalId.Value);
            }

            if (request.ServiceCategoryId.HasValue)
            {
                query = query.Where(s => s.ServiceCategoryId == request.ServiceCategoryId.Value);
            }

            if (request.MinPrice.HasValue)
            {
                query = query.Where(s => s.Price >= request.MinPrice.Value);
            }

            if (request.MaxPrice.HasValue)
            {
                query = query.Where(s => s.Price <= request.MaxPrice.Value);
            }

            var totalCount = await query.CountAsync();

            // Apply sorting - only support Name and Price
            var isDescending = string.Equals(request.SortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var sortBy = string.IsNullOrWhiteSpace(request.SortBy) ? "Name" : request.SortBy;

            var orderedQuery = sortBy.ToLower() switch
            {
                "name" => isDescending
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name),
                "price" => isDescending
                    ? query.OrderByDescending(s => s.Price)
                    : query.OrderBy(s => s.Price),
                _ => query.OrderBy(s => s.Name) // Default sorting by name
            };

            var services = await orderedQuery
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
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

        public async Task<List<Guid>> GetAllDistinctHospitalIdsAsync()
        {
            return await _context.Services
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
