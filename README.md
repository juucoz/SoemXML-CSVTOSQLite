## SoemXMLToSQLite

SoemXMLToSQLite is a .NET Core console app for reading appropriate information 
from XML files and saving it in a SQLite file.

## Installation

In order to build the project .NET Core 3.1 should be installed.

After that, build the project by using the .csproj file

dotnet build SoemXMLToSQLite.csproj

## Usage

When the build is complete, we can run the exe file by

SoemXMLToSQLite -i (inputPath) -m (sourceFileMask) -d (dbFilePath)

inputPath - This is the path that the files will be searched.
sourceFileMask - This is filter that will be used to determine which 
                 files are going to be selected.
dbFilePath - This is the path that the database will be created.

use example:

SoemXMLToSQLite -i C:\Users\ata.akcay\Desktop\inputFile -m *.xml -d soem.sqlite

If the input file path is found, the system asks which folder would you like to
select under the input file.
When you enter the name of the file, the filter applies and all of the files
under the file name you've entered gets saved in soem.sqlite.