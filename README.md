# plant-conv
Convert database with plant observations to SQL for web interface.

The project can be compiled with Visual Studio Community 2017.

The input is an Access database (.mdb), and the output an SQL file.

plantdbconv --in db.mdb --out data.sql

Before using the SQL, first the tables need to be created (using table-def.sql).
