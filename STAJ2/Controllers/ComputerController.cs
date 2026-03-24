using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Staj2.Infrastructure.Data;
using Staj2.Services.Interfaces;
using Staj2.Services.Models; // Request modellerini (DTO) görmek için
using STAJ2.Authorization;
using System.Globalization;
using System.Security.Claims;

namespace STAJ2.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComputerController : ControllerBase
{
    private readonly IComputerService _computerService;
    public ComputerController(IComputerService computerService)
    {
        _computerService = computerService;
    }

    // --- YARDIMCI METOTLAR (Controller içinde sürekli kod tekrarı yapmamak için) ---
    private int GetUserId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;
    private bool IsAdmin() => User.IsInRole("Yönetici");

    // 1. Cihaz Detayı
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetComputer(int id)
    {
        var result = await _computerService.GetComputerAsync(id, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();
        if (result.isNotFound) return NotFound();

        return Ok(result.data);
    }

    // 2. Disk Listesi
    [HttpGet("{computerId:int}/disks")]
    public async Task<IActionResult> GetComputerDisks(int computerId)
    {
        var result = await _computerService.GetComputerDisksAsync(computerId, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();

        return Ok(result.data);
    }

    // 3. Eşik Değerlerini Güncelle (0-100 Kontrolü Eklendi)
    [HttpPut("update-thresholds/{computerId:int}")]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        var result = await _computerService.UpdateThresholdsAsync(computerId, request, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();
        if (result.isNotFound) return NotFound();
        if (result.errorMessage != null) return BadRequest(result.errorMessage);

        return Ok();
    }

    // 4. Etiket Atama
    [HttpPut("{id}/tags")]
    public async Task<IActionResult> UpdateComputerTags(int id, [FromBody] UpdateComputerTagsRequest request)
    {
        var isSuccess = await _computerService.UpdateComputerTagsAsync(id, request);

        if (!isSuccess) return NotFound();

        return Ok();
    }

    // 5. İsim Değiştirme
    [HttpPut("update-display-name")]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateComputerNameRequest request)
    {
        var result = await _computerService.UpdateDisplayNameAsync(request);

        if (!result.isSuccess)
        {
            if (result.isNotFound) return NotFound(new { message = result.errorMessage });
            return BadRequest(new { message = result.errorMessage });
        }

        return Ok(new { message = "İsim başarıyla güncellendi." });
    }
    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir
    [HttpGet("{id:int}/metrics-history")]
    public async Task<IActionResult> GetMetricsHistory(int id, [FromQuery] string start, [FromQuery] string end)
    {
        var result = await _computerService.GetMetricsHistoryAsync(id, start, end);

        if (result.isBadRequest)
            return BadRequest(result.errorMessage);

        return Ok(result.data);
    }

    [HttpGet]
    public async Task<IActionResult> GetAllComputers()
    {
        var result = await _computerService.GetAllComputersAsync(GetUserId(), IsAdmin());
        return Ok(result);
    }

    // 8. Cihaz Silme (Sadece Pasif Olanlar İçin Soft Delete)
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var result = await _computerService.DeleteComputerAsync(id);

        if (result.isNotFound)
            return NotFound(new { message = result.errorMessage });

        if (result.isBadRequest)
            return BadRequest(new { message = result.errorMessage });

        return Ok(new { message = "Bilgisayar başarıyla silindi." });
    }


    //private async Task<bool> CheckComputerAccessAsync(int computerId)
    //{
    //    if (User.IsInRole("Yönetici")) return true;

    //    // 2. Kullanıcı ID'sini al
    //    var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    //    int userId = int.TryParse(userIdString, out int id) ? id : 0;
    //    if (userId == 0) return false;

    //    // 3. Doğrudan cihaz ataması var mı kontrol et
    //    bool hasDirectAccess = await _db.UserComputerAccesses
    //        .AnyAsync(uca => uca.UserId == userId && uca.ComputerId == computerId);
    //    if (hasDirectAccess) return true;

    //    // 4. Etiket üzerinden ataması var mı kontrol et
    //    var computerTagIds = await _db.Computers
    //        .Where(c => c.Id == computerId)
    //        .SelectMany(c => c.Tags.Select(t => t.Id))
    //        .ToListAsync();

    //    bool hasTagAccess = await _db.UserTagAccesses
    //        .AnyAsync(uta => uta.UserId == userId && computerTagIds.Contains(uta.TagId));

    //    return hasTagAccess;
    //}

    [HttpGet("tags")]
    public async Task<IActionResult> GetMyTags()
    {
        var tags = await _computerService.GetMyTagsAsync(GetUserId(), IsAdmin());
        return Ok(tags);
    }

}