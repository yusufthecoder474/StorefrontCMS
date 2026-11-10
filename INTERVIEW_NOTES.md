# StorefrontCMS interview notes

1. **Why MVC and stored procedures?** Razor MVC matches the target role and keeps rendering simple. Controllers bind HTTP input, services manage rules and cache/session behavior, and Dapper calls explicit stored procedures. There is no EF Core in the main flows.

2. **How do you prevent overselling?** PlaceOrder takes update/serializable locks on each requested product in ascending ID order and retains them through commit. It validates all stock before decrementing. A rejected line rolls back the entire transaction. ROWLOCK is a hint, so SQL Server can still escalate locks.

3. **Why check stock again at checkout?** Cart stock checks are only feedback. Another customer can order after the cart was updated. The database transaction is the final authority and calculates prices from Products, never from browser totals.

4. **What happens on double submit?** A unique customer/request-ID constraint plus a locked lookup returns the existing order for a repeated request. The session provides the checkout token. After a completed redirect the token is cleared; a later stale form is rejected. Recovery after a lost browser session is limited to order history.

5. **How is CMS rollback different from editing?** Editing inserts an immutable draft. Publishing changes a live pointer and records publication history. Rollback selects the latest different published version, never an unpublished draft. Repeated rollback can toggle the last two live versions.

6. **How does caching stay correct?** Published text is cached for two minutes. Reads and publication changes use a small shared process gate; successful writes evict the slug before releasing it. This handles an in-flight stale read in one process. It is not distributed invalidation.

7. **How do you protect the application?** SQL values are parameters, POSTs validate anti-forgery tokens, Razor encodes text, input models validate on the server, and admin actions require a role in a signed cookie. PasswordHasher stores password hashes. Local environment secrets are excluded from Git.

8. **What have you tested?** The included xUnit cases test order-service validation and repository forwarding; they do not prove SQL concurrency. The SQL script checks stock/price/audit/rollback/idempotency invariants with dedicated fixtures. Consult EXECUTION_REPORT.md for actual runs rather than claiming every supplied test passed.

9. **How would you handle a production incident or code review?** Start with the response correlation ID, inspect structured request and exception logs, then use the runbook's SQL checks. In a review I would focus on transaction boundaries, authorization, invalidation and reproducible evidence. The repo suggests Agile acceptance criteria and conventional commits; it does not claim a real team sprint or external review happened.

10. **What would you change for real production?** Use HTTPS and managed secrets, a least-privilege SQL login with EXECUTE grants, Identity with registration/reset/MFA, durable per-user carts, migrations, backups and restore drills. Add concurrency integration tests, distributed cache invalidation if scaling, centralized telemetry, bounded retries for transient DB errors, and an explicit order lifecycle. Real payment handling would require an independently designed payment workflow; this project intentionally has none.
