# UltimateAuth Argon2 Security Provider

Argon2 password hashing implementation for UltimateAuth.

This package provides a secure implementation of:

- `IUAuthPasswordHasher`

using the Argon2 algorithm.

---

## 🔐 Why Argon2?

Argon2 is a modern password hashing algorithm designed to resist:

- GPU attacks
- Parallel brute-force attacks
- Memory trade-off attacks

It is recommended for secure password storage in modern applications.

---

⚠️ Notes

- UltimateAuth doesn't have default password hasher. You need one of the security packages.
- You should use only one IUAuthPasswordHasher implementation.
