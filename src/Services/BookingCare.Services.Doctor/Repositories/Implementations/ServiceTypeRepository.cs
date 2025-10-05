using BookingCare.Services.Doctor.Data;
using BookingCare.Services.Doctor.Models.DTOs.Requests;
using BookingCare.Services.Doctor.Models.Entities;
using BookingCare.Services.Doctor.Repositories.Interfaces;
using BookingCare.Shared.Common.Enums;
using Microsoft.EntityFrameworkCore;

namespace BookingCare.Services.Doctor.Repositories.Implementations;

public class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly DoctorDbContext _context;

    public ServiceTypeRepository(DoctorDbContext context)
    {
        _context = context;
    }

    #region ServiceType CRUD Operations

    public async Task<ServiceTypeEntity?> GetServiceTypeByIdAsync(Guid id)
    {
        return await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Id == id);
    }

    public async Task<ServiceTypeEntity?> GetServiceTypeByNameAsync(string name)
    {
        return await _context.ServiceTypes
            .FirstOrDefaultAsync(st => st.Name == name);
    }

    public async Task<ServiceTypeEntity> CreateServiceTypeAsync(ServiceTypeEntity serviceType)
    {
        _context.ServiceTypes.Add(serviceType);
        await _context.SaveChangesAsync();
        return serviceType;
    }

    public async Task<ServiceTypeEntity> UpdateServiceTypeAsync(ServiceTypeEntity serviceType)
    {
        _context.ServiceTypes.Update(serviceType);
        await _context.SaveChangesAsync();
        return serviceType;
    }

    public async Task<bool> DeleteServiceTypeAsync(Guid id)
    {
        var serviceType = await GetServiceTypeByIdAsync(id);
        if (serviceType == null) return false;

        // Soft delete - chỉ thay đổi status thành INACTIVE
        serviceType.Status = Status.INACTIVE;
        serviceType.UpdatedAt = DateTime.UtcNow;
        _context.ServiceTypes.Update(serviceType);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ServiceTypeExistsAsync(Guid id)
    {
        return await _context.ServiceTypes
            .AnyAsync(st => st.Id == id);
    }

    public async Task<bool> ServiceTypeNameExistsAsync(string name, Guid? excludeId = null)
    {
        var query = _context.ServiceTypes.Where(st => st.Name == name);

        if (excludeId.HasValue)
        {
            query = query.Where(st => st.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    #endregion

    #region ServiceType Query Operations

    public async Task<(List<ServiceTypeEntity> ServiceTypes, int TotalCount)> GetServiceTypesAsync(ServiceTypeQueryRequest query)
    {
        var queryable = _context.ServiceTypes.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(st =>
                st.Name.ToLower().Contains(searchTerm) ||
                (st.Description != null && st.Description.ToLower().Contains(searchTerm)));
        }

        // Apply status filter
        if (query.Status.HasValue)
        {
            queryable = queryable.Where(st => st.Status == query.Status.Value);
        }

        // Sort
        if (!string.IsNullOrEmpty(query.SortBy))
        {
            if (query.SortBy == "Name")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(st => st.Name) : queryable.OrderBy(st => st.Name);
            else if (query.SortBy == "CreatedAt")
                queryable = query.SortOrder == "desc" ? queryable.OrderByDescending(st => st.CreatedAt) : queryable.OrderBy(st => st.CreatedAt);
        }
        else
        {
            queryable = queryable.OrderBy(st => st.Name);
        }

        // Get total count
        var totalCount = await queryable.CountAsync();

        // Apply pagination
        var serviceTypes = await queryable
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return (serviceTypes, totalCount);
    }

    public async Task<List<ServiceTypeEntity>> GetAllServiceTypesAsync()
    {
        return await _context.ServiceTypes
            .OrderBy(st => st.Name)
            .ToListAsync();
    }

    public IQueryable<ServiceTypeEntity> GetQueryableServiceTypes()
    {
        return _context.ServiceTypes.AsQueryable();
    }

    #endregion
}
