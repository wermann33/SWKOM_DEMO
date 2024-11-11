﻿using ASP_Rest_API.DTO;
using AutoMapper;
using TodoDAL.Entities;

namespace ASP_Rest_API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<TodoItem, TodoItemDto>()
                .ForMember(dest => dest.Id, opt
                    => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt
                    => opt.MapFrom(src => $"*{src.Name ?? string.Empty}*"))
                .ForMember(dest => dest.IsComplete, opt
                    => opt.MapFrom(src => src.IsComplete))
                .ForMember(dest => dest.FileName, opt 
                    => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.OcrText, opt 
                    => opt.MapFrom(src => src.OcrText))
                .ReverseMap()
                .ForMember(dest => dest.Id, opt
                    => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Name, opt
                    => opt.MapFrom(src => (src.Name ?? string.Empty).Replace("*", "")))
                .ForMember(dest => dest.IsComplete, opt
                    => opt.MapFrom(src => src.IsComplete))
                .ForMember(dest => dest.FileName, opt 
                    => opt.MapFrom(src => src.FileName))
                .ForMember(dest => dest.OcrText, opt 
                    => opt.MapFrom(src => src.OcrText));



        }
    }
}
