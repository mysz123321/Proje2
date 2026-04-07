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
    private readonly IConfiguration _config;

    public ComputerController(IComputerService computerService, IConfiguration config)
    {
        _computerService = computerService;
        _config = config;
    }

    // --- YARDIMCI METOTLAR ---
    private int GetUserId() => int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out int id) ? id : 0;

    private bool IsAdmin()
    {
        var adminRoleName = _config["AppDefaults:AdminRoleName"] ?? "Yönetici";
        return User.IsInRole(adminRoleName);
    }

    // 1. Cihaz Detayı
    [HttpGet("{id:int}")]
    [HasPermission(AppPermissions.Computer_Read)]
    public async Task<IActionResult> GetComputer(int id)
    {
        var result = await _computerService.GetComputerAsync(id, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();
        if (result.isNotFound) return NotFound();

        return Ok(result.data);
    }

    // 2. Disk Listesi
    [HttpGet("{computerId:int}/disks")]
    [HasPermission(AppPermissions.Computer_Read)]
    public async Task<IActionResult> GetComputerDisks(int computerId)
    {
        var result = await _computerService.GetComputerDisksAsync(computerId, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();

        return Ok(result.data);
    }

    // 3. Eşik Değerlerini Güncelle
    [HttpPut("update-thresholds/{computerId:int}")]
    [HasPermission(AppPermissions.Computer_SetThreshold)]
    public async Task<IActionResult> UpdateThresholds(int computerId, [FromBody] UpdateThresholdsRequest request)
    {
        var result = await _computerService.UpdateThresholdsAsync(computerId, request, GetUserId(), IsAdmin());

        if (result.isForbidden) return Forbid();
        if (result.isNotFound) return NotFound(new { message = result.message });
        if (result.isBadRequest) return BadRequest(new { message = result.message });

        // Sabit yazı yerine Servis'ten gelen mesajı dönüyoruz
        return Ok(new { message = result.message });
    }

    // 4. Etiket Atama
    [HttpPut("{id}/tags")]
    [HasPermission(AppPermissions.Computer_AssignTag)]
    public async Task<IActionResult> UpdateComputerTags(int id, [FromBody] UpdateComputerTagsRequest request)
    {
        var result = await _computerService.UpdateComputerTagsAsync(id, request);

        if (result.isNotFound) return NotFound(new { message = result.message });

        // Sabit yazı yerine Servis'ten gelen mesajı dönüyoruz
        return Ok(new { message = result.message });
    }

    // 5. İsim Değiştirme
    [HttpPut("update-display-name")]
    [HasPermission(AppPermissions.Computer_Rename)]
    public async Task<IActionResult> UpdateDisplayName([FromBody] UpdateComputerNameRequest request)
    {
        var result = await _computerService.UpdateDisplayNameAsync(request);

        if (!result.isSuccess)
        {
            if (result.isNotFound) return NotFound(new { message = result.message });
            return BadRequest(new { message = result.message });
        }

        // Sabit yazı yerine Servis'ten gelen mesajı dönüyoruz
        return Ok(new { message = result.message });
    }

    // 6. Belirli bir tarih aralığındaki metrik geçmişini getir
    [HttpGet("{id:int}/metrics-history")]
    [HasPermission(AppPermissions.Computer_Filter)]
    public async Task<IActionResult> GetMetricsHistory(int id, [FromQuery] string start, [FromQuery] string end)
    {
        var result = await _computerService.GetMetricsHistoryAsync(id, start, end);

        if (result.isBadRequest)
            return BadRequest(new { message = result.errorMessage });

        return Ok(result.data);
    }

    // 7. Tüm Cihazları Getir
    [HttpGet]
    [HasPermission(AppPermissions.Computer_Read)]
    public async Task<IActionResult> GetAllComputers()
    {
        var result = await _computerService.GetAllComputersAsync(GetUserId(), IsAdmin());
        return Ok(result);
    }

    // 8. Cihaz Silme (Sadece Pasif Olanlar İçin Soft Delete)
    [HttpDelete("{id:int}")]
    [HasPermission(AppPermissions.Computer_Delete)]
    public async Task<IActionResult> DeleteComputer(int id)
    {
        var result = await _computerService.DeleteComputerAsync(id);

        if (result.isNotFound)
            return NotFound(new { message = result.message });

        if (result.isBadRequest)
            return BadRequest(new { message = result.message });

        // Sabit yazı yerine Servis'ten gelen mesajı dönüyoruz
        return Ok(new { message = result.message });
    }

    // 9. Etiketleri Getir
    [HttpGet("tags")]
    [HasPermission(AppPermissions.None)]
    public async Task<IActionResult> GetMyTags()
    {
        var tags = await _computerService.GetMyTagsAsync(GetUserId(), IsAdmin());
        return Ok(tags);
    }

    [HttpGet("performance-report")]
    public async Task<IActionResult> GetPerformanceReport()
    {
        var report = await _computerService.GetPerformanceReportAsync(GetUserId(), IsAdmin());
        return Ok(report);
    }

    [HttpGet("{id:int}/metrics-summary")]
    public async Task<IActionResult> GetMetricsSummary(int id, [FromQuery] string metricType, [FromQuery] string? diskName = null)
    {
        var result = await _computerService.GetMetricsSummaryAsync(id, metricType, diskName);
        return Ok(result);
    }
}