# Planning and evidence package

- [ERD image](RaceDay_ERD.png) and [editable SVG](RaceDay_ERD.svg)
- [RaceDay endpoint plan](RaceDay_Endpoint_Plan.md)
- [SQL Server schema and sample data](RaceDay_Schema.sql)
- [Requirements audit](REQUIREMENTS-AUDIT.md)
- [Exported EF model metadata](schema.json)
- [Validation results](VALIDATION.md)

The current 11-table schema includes seven ASP.NET Identity tables and four race-domain tables. This reconciled package matches the current API. It documents the differences from the older Part 1 material rather than claiming the original submission was already correct.

Regenerate schema metadata and SQL with:
```sh
dotnet run --project tools/SchemaExporter -- docs
```
The ERD is based on schema.json. SQL sample users use real Identity password hashes; their demo password is Coursework@123. Use a new, empty database for the standalone SQL script. It deliberately refuses to overwrite existing planning data.
