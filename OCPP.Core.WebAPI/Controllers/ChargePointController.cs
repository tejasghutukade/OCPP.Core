using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OCPP.Core.Database;
using OCPP.Core.WebAPI.Dtos;

namespace OCPP.Core.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChargePointController: ControllerBase
{
    private readonly ILogger<ChargePointController> _logger;
    private readonly OcppCoreContext _dbContext;
    private static Random random = new Random();
    private readonly IMapper _mapper;

    public ChargePointController(ILogger<ChargePointController> logger,OcppCoreContext dbContext,IMapper mapper)

    {
        _logger = logger;
        _dbContext = dbContext;
        _mapper = mapper;
    }

    [HttpGet]
    public IEnumerable<ChargePointDto> Get()
    {
        var list = _dbContext.ChargePoints.ToList();
        var mappedList = _mapper.Map<List<ChargePointDto>>(list);
        return mappedList;
    }
    
    [HttpGet]
    [Route("{id}")]
    public ChargePointDto Get(string id)
    {
        var list = _dbContext.ChargePoints.Where(x=>x.ChargePointId == id).Take(1).FirstOrDefault();
        var mappedList = _mapper.Map<ChargePointDto>(list);
        return mappedList;
    }
    
    [HttpPost]
    public IActionResult Post(ChargePointDto? chargePointDto)
    {
        try
        {
            if (chargePointDto == null)
            {
                return BadRequest();
            }
            var chargePoint = _mapper.Map<ChargePoint>(chargePointDto);
            chargePoint.ChargePointId = RandomStringForChargePoint(7); 
            _dbContext.ChargePoints.Add(chargePoint);
            _dbContext.SaveChanges();
            return Ok(chargePoint);
        }catch(Exception ex)
        {
            _logger.LogError(ex, "Error while adding charge point");
            return BadRequest();
        }
    }
    
    private string RandomStringForChargePoint(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var id = new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        if(_dbContext.ChargePoints.Any(x=>x.ChargePointId == id))
        {
            return RandomStringForChargePoint(length);
        }
        return id;
    }
}