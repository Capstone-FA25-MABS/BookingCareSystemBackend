using BookingCare.Services.ServiceMedical.Constants;
using BookingCare.Services.ServiceMedical.Data;
using BookingCare.Services.ServiceMedical.Models.Entities;
using BookingCare.Services.ServiceMedical.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.ServiceMedical.Repositories.Implementations
{
    public class ServiceCategoryRepository : IServiceCategoryRepository
    {
        private readonly ServiceMedicalDbContext _context;

        public ServiceCategoryRepository(ServiceMedicalDbContext context)
        {
            _context = context;
        }

        public async Task<ServiceCategoryEntity> CreateAsync(ServiceCategoryEntity entity)
        {
            _context.ServiceCategories.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<ServiceCategoryEntity?> GetByIdAsync(Guid id)
        {
            return await _context.ServiceCategories
                .Include(sc => sc.Parent)
                .Include(sc => sc.Children)
                .FirstOrDefaultAsync(sc => sc.Id == id);
        }

        public async Task<ServiceCategoryEntity> UpdateAsync(ServiceCategoryEntity entity)
        {
            _context.ServiceCategories.Update(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var entity = await _context.ServiceCategories.FindAsync(id);
            if (entity == null) return false;

            // Soft delete: Only change status to INACTIVE instead of hard delete
            entity.Status = StatusConstants.Inactive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ServiceCategoryEntity>> GetAllAsync()
        {
            return await _context.ServiceCategories
                .Include(sc => sc.Parent)
                .Include(sc => sc.Children)
                .OrderBy(sc => sc.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceCategoryEntity>> GetParentCategoriesAsync()
        {
            return await _context.ServiceCategories
                .Where(sc => sc.ParentId == null && sc.Status == StatusConstants.Active)
                .Include(sc => sc.Children.Where(c => c.Status == StatusConstants.Active))
                .OrderBy(sc => sc.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceCategoryEntity>> GetChildrenAsync(Guid parentId)
        {
            return await _context.ServiceCategories
                .Where(sc => sc.ParentId == parentId && sc.Status == StatusConstants.Active)
                .OrderBy(sc => sc.Name)
                .ToListAsync();
        }

        public async Task<List<ServiceCategoryEntity>> GetActiveCategoriesAsync()
        {
            return await _context.ServiceCategories
                .Where(sc => sc.Status == StatusConstants.Active)
                .Include(sc => sc.Parent)
                .OrderBy(sc => sc.Name)
                .ToListAsync();
        }

        public async Task<(List<ServiceCategoryEntity> Categories, int TotalCount)> GetPagedAsync(
            int page, int pageSize, string? searchTerm = null, string? status = null, Guid? parentId = null,
            string? sortBy = null, string? sortDirection = null)
        {
            // No need to include Parent or Children for flat list - frontend will handle hierarchy
            var query = _context.ServiceCategories
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(sc => sc.Name.Contains(searchTerm) ||
                                         (sc.Description != null && sc.Description.Contains(searchTerm)));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(sc => sc.Status == status);
            }

            if (parentId.HasValue)
            {
                query = query.Where(sc => sc.ParentId == parentId.Value);
            }

            var totalCount = await query.CountAsync();

            // Apply sorting - only support Name
            var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
            var sortField = string.IsNullOrWhiteSpace(sortBy) ? "Name" : sortBy;

            var orderedQuery = sortField.ToLower() switch
            {
                "name" => isDescending
                    ? query.OrderByDescending(sc => sc.Name)
                    : query.OrderBy(sc => sc.Name),
                _ => query.OrderBy(sc => sc.Name) // Default sorting by name
            };

            var categories = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (categories, totalCount);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ServiceCategories.AnyAsync(sc => sc.Id == id);
        }

        public async Task<bool> HasChildrenAsync(Guid id)
        {
            return await _context.ServiceCategories.AnyAsync(sc => sc.ParentId == id);
        }

        public async Task<bool> IsValidParentAsync(Guid parentId, Guid childId)
        {
            // Check if parent exists and is not the same as child
            if (parentId == childId) return false;

            var parent = await _context.ServiceCategories.FindAsync(parentId);
            if (parent == null) return false;

            // Check for circular reference - child should not be an ancestor of parent
            var currentParent = parent;
            while (currentParent?.ParentId != null)
            {
                if (currentParent.ParentId == childId) return false;
                currentParent = await _context.ServiceCategories.FindAsync(currentParent.ParentId);
            }

            return true;
        }

        public async Task<List<ServiceCategoryEntity>> GetCategoryHierarchyAsync(Guid categoryId)
        {
            var hierarchy = new List<ServiceCategoryEntity>();
            var current = await _context.ServiceCategories
                .Include(sc => sc.Parent)
                .FirstOrDefaultAsync(sc => sc.Id == categoryId);

            while (current != null)
            {
                hierarchy.Insert(0, current);
                if (current.ParentId == null) break;
                current = current.Parent;
            }

            return hierarchy;
        }

        public IQueryable<ServiceCategoryEntity> GetQueryable()
        {
            return _context.ServiceCategories.AsQueryable();
        }
    }
}
