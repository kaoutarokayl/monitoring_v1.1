using Microsoft.AspNetCore.Mvc;
using Microsoft.Playwright;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace KtcWeb.API.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardExportController(IConfiguration configuration) : ControllerBase
    {
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportPdf(
            [FromQuery] string dashboardUid,
            [FromQuery] string slug,
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string title = "KTC Dashboard Reporting",
            [FromQuery] string subtitle = "Rapport de supervision")
        {
            var grafanaBaseUrl = configuration["Grafana:BaseUrl"] ?? "http://localhost:3000";
            var generatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var url = $"{grafanaBaseUrl}/d/{dashboardUid}/{slug}?orgId=1&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&timezone=browser&refresh=30s&theme=light&kiosk";

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                var page = await browser.NewPageAsync(new BrowserNewPageOptions
                {
                    ViewportSize = new ViewportSize { Width = 1600, Height = 1200 }
                });

                await page.EmulateMediaAsync(new PageEmulateMediaOptions
                {
                    ColorScheme = ColorScheme.Light
                });

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load
                });

                await page.AddStyleTagAsync(new PageAddStyleTagOptions
                {
                    Content = @"
                        html, body, .grafana-app, .dashboard-page, .react-grid-layout, .react-grid-item, .main-view, .dashboard-grid {
                            background-color: #ffffff !important;
                            font-family: Arial, Helvetica, sans-serif !important;
                        }
                        .sidemenu, .toolbar, .dashboard-header, .top-nav, .navbar, .page-header, .react-resizable-handle {
                            display: none !important;
                        }
                        .dashboard-page {
                            padding: 4px 0 0 0 !important;
                            max-width: 100% !important;
                        }
                        .react-grid-item {
                            box-shadow: none !important;
                            border: 1px solid #E5E7EB !important;
                            border-radius: 10px !important;
                            background: #ffffff !important;
                        }
                    "
                });

                await page.EvaluateAsync("document.documentElement.style.backgroundColor='white'; document.body.style.backgroundColor='white';");

                await page.WaitForTimeoutAsync(3000);

                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    Landscape = false,
                    DisplayHeaderFooter = true,
                    PreferCSSPageSize = true,
                    Scale = 0.96f,
                    HeaderTemplate = $@"<div style='width: 100%; font-size: 10px; color: #14532D; padding: 0 10mm; display: flex; align-items: center; justify-content: space-between; font-family: Arial, Helvetica, sans-serif; font-weight: 600;'>
                        <span style='color:#14532D;'>{Uri.EscapeDataString(title)}</span>
                        <span style='color:#4B5563;'>Période : {Uri.EscapeDataString(from)} → {Uri.EscapeDataString(to)}</span>
                        <span style='color:#4B5563;'>Généré le {Uri.EscapeDataString(generatedAt)}</span>
                    </div>",
                    FooterTemplate = @"<div style='width: 100%; font-size: 9px; color: #4B5563; padding: 0 10mm; display: flex; align-items: center; justify-content: space-between; font-family: Arial, Helvetica, sans-serif;'>
                        <span style='font-weight: 600; color: #14532D;'>Rapport KTC</span>
                        <span>Page <span class='pageNumber'></span>/<span class='totalPages'></span></span>
                    </div>",
                    Margin = new Margin
                    {
                        Top = "12mm",
                        Right = "8mm",
                        Bottom = "12mm",
                        Left = "8mm"
                    }
                });

                return File(pdfBytes, "application/pdf", $"dashboard-{slug}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "PDF export failed", detail = ex.Message });
            }
        }

        [HttpGet("export-pdf-with-charts")]
        public async Task<IActionResult> ExportPdfWithCharts(
            [FromQuery] string dashboardUid,
            [FromQuery] string slug,
            [FromQuery] string from,
            [FromQuery] string to,
            [FromQuery] string title = "KTC Monitoring",
            [FromQuery] string subtitle = "Rapport de supervision")
        {
            var grafanaBaseUrl = configuration["Grafana:BaseUrl"] ?? "http://localhost:3000";
            var generatedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
            var url = $"{grafanaBaseUrl}/d/{dashboardUid}/{slug}?orgId=1&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&timezone=browser&refresh=30s&theme=light&kiosk";

            try
            {
                using var playwright = await Playwright.CreateAsync();
                await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

                var page = await browser.NewPageAsync(new BrowserNewPageOptions
                {
                    ViewportSize = new ViewportSize { Width = 1600, Height = 1200 }
                });

                await page.EmulateMediaAsync(new PageEmulateMediaOptions
                {
                    ColorScheme = ColorScheme.Light
                });

                await page.GotoAsync(url, new PageGotoOptions
                {
                    WaitUntil = WaitUntilState.Load
                });

                await page.AddStyleTagAsync(new PageAddStyleTagOptions
                {
                    Content = @"
                        html, body, .grafana-app, .dashboard-page, .react-grid-layout, .react-grid-item, .main-view, .dashboard-grid {
                            background-color: #ffffff !important;
                            font-family: Arial, Helvetica, sans-serif !important;
                        }
                        .sidemenu, .toolbar, .dashboard-header, .top-nav, .navbar, .page-header, .react-resizable-handle {
                            display: none !important;
                        }
                        .dashboard-page {
                            padding: 4px 0 0 0 !important;
                            max-width: 100% !important;
                        }
                        .react-grid-item {
                            box-shadow: none !important;
                            border: 1px solid #E5E7EB !important;
                            border-radius: 10px !important;
                            background: #ffffff !important;
                        }
                    "
                });

                await page.EvaluateAsync("document.documentElement.style.backgroundColor='white'; document.body.style.backgroundColor='white';");

                await page.WaitForTimeoutAsync(3000);

                // Capture the dashboard as an image
                var screenshot = await page.ScreenshotAsync(new PageScreenshotOptions
                {
                    FullPage = true
                });

                // Generate PDF with custom content
                var pdfBytes = await page.PdfAsync(new PagePdfOptions
                {
                    Format = PaperFormat.A4,
                    PrintBackground = true,
                    Landscape = false,
                    DisplayHeaderFooter = true,
                    PreferCSSPageSize = true,
                    Scale = 0.96f,
                    HeaderTemplate = $@"<div style='width: 100%; font-size: 10px; color: #14532D; padding: 0 10mm; display: flex; align-items: center; justify-content: space-between; font-family: Arial, Helvetica, sans-serif; font-weight: 600;'>
                        <span style='color:#14532D;'>{Uri.EscapeDataString(title)}</span>
                        <span style='color:#4B5563;'>Période : {Uri.EscapeDataString(from)} → {Uri.EscapeDataString(to)}</span>
                        <span style='color:#4B5563;'>Généré le {Uri.EscapeDataString(generatedAt)}</span>
                    </div>",
                    FooterTemplate = @"<div style='width: 100%; font-size: 9px; color: #4B5563; padding: 0 10mm; display: flex; align-items: center; justify-content: space-between; font-family: Arial, Helvetica, sans-serif;'>
                        <span style='font-weight: 600; color: #14532D;'>Rapport KTC</span>
                        <span>Page <span class='pageNumber'></span>/<span class='totalPages'></span></span>
                    </div>",
                    Margin = new Margin
                    {
                        Top = "12mm",
                        Right = "8mm",
                        Bottom = "12mm",
                        Left = "8mm"
                    }
                });

                return File(pdfBytes, "application/pdf", $"dashboard-{slug}-with-charts.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "PDF export with charts failed", detail = ex.Message });
            }
        }
    }
}
