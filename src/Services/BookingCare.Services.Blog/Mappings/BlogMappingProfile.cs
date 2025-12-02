using AutoMapper;
using BookingCare.Services.Blog.Models.DTOs;
using BookingCare.Services.Blog.Models.Entities;

namespace BookingCare.Services.Blog.Mappings;

public class BlogMappingProfile : Profile
{
    public BlogMappingProfile()
    {
        CreateMap<BlogCategoryEntity, BlogCategoryDto>()
            .ForMember(dest => dest.Children, opt => opt.MapFrom(src => src.Children));

        CreateMap<CreateBlogCategoryRequest, BlogCategoryEntity>();
        CreateMap<UpdateBlogCategoryRequest, BlogCategoryEntity>();

        CreateMap<BlogEntity, BlogSummaryDto>()
            .ForMember(dest => dest.CreatedByName, opt => opt.Ignore()); // Will be populated manually in service
        CreateMap<BlogEntity, BlogDetailDto>()
            .ForMember(dest => dest.RelatedBlogs, opt => opt.MapFrom(src => Array.Empty<BlogSummaryDto>())); // Will be set manually in service

        CreateMap<CreateBlogRequest, BlogEntity>();
        CreateMap<UpdateBlogRequest, BlogEntity>();
    }
}

