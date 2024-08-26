using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<AppUser, MemberDto>()
            .ForMember(dest => dest.Age, opt =>
                opt.MapFrom(src => src.DateOfBirth.CalculateAge()))
            .ForMember(dest => dest.PhotoUrl, opt =>
                opt.MapFrom(src => src.Photos.FirstOrDefault(p => p.IsMain)!.Url));

        CreateMap<MemberUpdateDto, AppUser>();

        CreateMap<Photo, PhotoDto>();

        CreateMap<RegisterDto, AppUser>();

        CreateMap<string, DateOnly>().ConvertUsing(s => DateOnly.Parse(s));

        CreateMap<Message, MessageDto>()
            .ForMember(dest => dest.SenderPhotoUrl, opt =>
                opt.MapFrom(src => src.Sender.Photos.FirstOrDefault(p => p.IsMain)!.Url))
            .ForMember(dest => dest.RecipientPhotoUrl, opt =>
                opt.MapFrom(src => src.Recipient.Photos.FirstOrDefault(p => p.IsMain)!.Url));
    }
}
