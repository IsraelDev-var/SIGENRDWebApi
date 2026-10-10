using Microsoft.EntityFrameworkCore;
using SIGENRD.Core.Domain.Entities;
using SIGENRD.Core.Domain.Enums;
using SIGENRD.Core.Domain.Repositories;
using SIGENRD.Infrastructure.Persistences.Contexts;

namespace SIGENRD.Infrastructure.Persistences.Repositories
{
    public class CustomerRepository(AppContextSIGENRD context) : ICustomerRepository
    {
        public Task<Customer?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.ApplicationUserId == userId, cancellationToken);

        public Task<bool> NationalIdExistsAsync(string nationalIdOrRnc, CancellationToken cancellationToken = default)
            => context.Customers.AnyAsync(c => c.NationalIdOrRnc == nationalIdOrRnc, cancellationToken);

        public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
            => await context.Customers.AddAsync(customer, cancellationToken);
    }

    public class InstallerCompanyRepository(AppContextSIGENRD context) : IInstallerCompanyRepository
    {
        public Task<InstallerCompany?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
            => context.InstallerCompanies.AsNoTracking().FirstOrDefaultAsync(c => c.ApplicationUserId == userId, cancellationToken);

        public Task<InstallerCompany?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
            => context.InstallerCompanies.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        public Task<bool> RncExistsAsync(string rnc, CancellationToken cancellationToken = default)
            => context.InstallerCompanies.AnyAsync(c => c.Rnc == rnc, cancellationToken);

        public async Task<IReadOnlyList<InstallerCompany>> ListAsync(InstallerValidationStatus? status, CancellationToken cancellationToken = default)
        {
            var query = context.InstallerCompanies.AsNoTracking();

            if (status.HasValue)
                query = query.Where(c => c.ValidationStatus == status.Value);

            return await query.OrderBy(c => c.TradeName).ToListAsync(cancellationToken);
        }

        public async Task AddAsync(InstallerCompany company, CancellationToken cancellationToken = default)
            => await context.InstallerCompanies.AddAsync(company, cancellationToken);
    }

    public class NotificationRepository(AppContextSIGENRD context) : INotificationRepository
    {
        public async Task<PagedResult<Notification>> ListAsync(
            string userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            var query = context.Notifications.AsNoTracking().Where(n => n.UserId == userId);

            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(n => n.CreatedAt).ThenByDescending(n => n.Id)
                .Skip((page - 1) * pageSize).Take(pageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<Notification>(items, page, pageSize, total);
        }

        public Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default)
            => context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        public Task<Notification?> GetForUserAsync(int id, string userId, CancellationToken cancellationToken = default)
            => context.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);

        public Task<int> MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
            => context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
