using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EVSwap.API.Core.DTOs.Wallet;
using EVSwap.API.Core.Interfaces.Services;

namespace EVSwap.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;

    public WalletController(IWalletService walletService)
    {
        _walletService = walletService;
    }

    private int GetUserId() => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            var wallet = await _walletService.GetBalanceAsync(GetUserId());
            return Ok(wallet);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddMoney([FromBody] AddMoneyRequest request)
    {
        try
        {
            var wallet = await _walletService.AddMoneyAsync(GetUserId(), request);
            return Ok(wallet);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions()
    {
        try
        {
            var transactions = await _walletService.GetTransactionsAsync(GetUserId());
            return Ok(transactions);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }
}
