using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.UnitTests.Integration;

/// <summary>
/// Hay un perfil de cliente y una empresa por usuario (índices únicos): las pruebas reutilizan el del usuario de
/// prueba en lugar de crear uno nuevo cada vez.
/// </summary>
public static class TestData
{
    public static string Unique() => Guid.NewGuid().ToString("N")[..8];

    public static async Task<Customer> CustomerForAsync(
        AppContextSIGENRD context, string userId, bool legalEntity = false, string? email = null)
    {
        var customer = await context.Customers.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

        if (customer is null)
        {
            customer = new Customer
            {
                ApplicationUserId = userId,
                FullName = "Cliente de prueba",
                NationalIdOrRnc = Unique() + "000"
            };
            context.Customers.Add(customer);
        }

        customer.IsLegalEntity = legalEntity;
        customer.Email = email;
        await context.SaveChangesAsync();
        return customer;
    }

    public static async Task<InstallerCompany> CompanyForAsync(AppContextSIGENRD context, string userId, bool validated = true)
    {
        var company = await context.InstallerCompanies.FirstOrDefaultAsync(c => c.ApplicationUserId == userId);

        if (company is null)
        {
            company = new InstallerCompany
            {
                TradeName = "Instaladora " + Unique(),
                Rnc = Unique(),
                ApplicationUserId = userId
            };
            context.InstallerCompanies.Add(company);
        }

        company.ValidationStatus = validated ? InstallerValidationStatus.Validated : InstallerValidationStatus.Pending;
        await context.SaveChangesAsync();
        return company;
    }
}
