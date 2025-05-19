# PostgreSQL DB Diff Checker 🔍

An ASP.NET Core MVC tool to visually compare two PostgreSQL databases and identify schema differences across multiple dimensions.

---

### 📌 Project Purpose

This tool is designed for database administrators, developers, and QA engineers who need to:

- Compare **structural differences** between PostgreSQL databases.
- Validate **changes across environments** (e.g., dev vs. staging).
- Ensure **schema consistency** in CI/CD workflows.
- Prepare migration reports and regression checks.

---

### 🚀 Key Features

- Detect differences in:
  - Schemas
  - Tables and columns
  - Constraints (primary, unique, foreign keys)
  - Indexes
  - Views (`CREATE VIEW` definitions)
- Clean and intuitive **Bootstrap-powered UI**
- Grouped results using collapsible accordions
- Visual indicators: Added, Removed, Modified
- Ready for further extension (Functions, Procedures, Data Diff)

---

### 🧰 Technologies Used

- ASP.NET Core MVC (.NET 6+)
- Razor View Engine
- PostgreSQL with Npgsql
- Bootstrap 5 (UI)
- pg_catalog & information_schema introspection queries

---

### 📁 Project Structure

DbDiffChecker/

├── DbDiffChecker.Web # MVC front-end application

├── DbDiffChecker.Core # Models and business logic

---

### 📸 UI Preview

![image](https://github.com/user-attachments/assets/7033b42c-32a1-46fe-9536-27f1cc2077b9)
![image](https://github.com/user-attachments/assets/867fce02-3710-4121-a7ca-bf6c3de896e5)

---

### 📊 Comparison Categories

Each comparison result is grouped and displayed in an accordion format:
- Schema Differences – added or removed schemas
- Table Differences – missing or extra tables
- View Differences – pg_get_viewdef() comparison
- Column Differences – type, nullability, default value changes
- Constraint Differences – PKs, FKs, UNIQUE constraints
- Index Differences – definition-based differences

---

### 🔮 Upcoming Features
 Compare PostgreSQL Functions via pg_get_functiondef()
 - Compare stored Procedures
 - Row-level data diffing using checksums or hashes
 - Export reports to PDF / Excel
 - CLI interface for automation pipelines

---

### 🤝 Contribution Guidelines
We welcome community contributions!
- Fork the repository
- Create a branch: feature/your-feature-name
- Commit your changes
- Open a pull request

Bug reports and suggestions are also appreciated.
