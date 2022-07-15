using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Database;

namespace OCPP.Core.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChargeTagController: ControllerBase
{
    private readonly ILogger<ChargeTagController> _logger;
    private readonly OcppCoreContext _dbContext;
    private readonly IMapper _mapper;

    public ChargeTagController(ILogger<ChargeTagController> logger,OcppCoreContext dbContext,IMapper mapper)

    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }
    
    
}