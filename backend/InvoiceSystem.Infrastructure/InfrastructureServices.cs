using InvoiceSystem.Application.Services;
using InvoiceSystem.Application.Services.Sales;
using InvoiceSystem.Infrastructure.Services;
using InvoiceSystem.Infrastructure.Services.Sales;
using Microsoft.Extensions.DependencyInjection;
using InvoiceSystem.Application.Common.Interfaces;

namespace InvoiceSystem.Infrastructure;

public static class InfrastructureServices
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services)
    {
        // Application のサービス実装
        services.AddScoped<IInvoiceService, InvoiceService>();

        // 売上一覧（/api/sales）
        services.AddScoped<ISalesService, SalesService>();

        services.AddScoped<IAdminSummaryService, AdminSummaryService>();

        // Reminder の業務処理
        // API内Worker / 外部Batchの双方から利用する
        services.AddScoped<IReminderJobProcessor, ReminderJobProcessor>();

        return services;
    }

    public static IServiceCollection AddReminderJobWorker(
        this IServiceCollection services)
    {
        services.AddHostedService<ReminderJobWorker>();

        return services;
    }
}