using AutoMapper;
using OCPP.Core.Database;
using OCPP.Core.WebAPI.Dtos;

namespace OCPP.Core.WebAPI.Helpers;

public class AutoMapperProfiles : Profile
{
    public AutoMapperProfiles()
    {
        CreateMap<ChargePoint, ChargePointDto>().ReverseMap(); 
    }
}