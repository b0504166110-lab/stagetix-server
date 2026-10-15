using AutoMapper;
using StageTix.Core.DTOs.Auth;
using StageTix.Core.Entities;

namespace StageTix.Service.Mapping;

public class UserProfile : Profile
{
    public UserProfile()
    {
        // UserResponse has no PasswordHash property, so the hash can never be mapped out.
        CreateMap<User, UserResponse>();
    }
}
