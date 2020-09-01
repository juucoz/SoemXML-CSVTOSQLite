# SoemXMLToSQLite

SoemXMLToSQLite is a .NET Core console app for reading appropriate information 
from XML and CSV files and saving it in a SQLite file.

## Installation

### Linux-x64 --self-contained
Download the linux-64 artifact in [pipelines](../../../pipelines) and extract it.
```
./bin/Release/netcoreapp3.1/linux-x64/publish/SoemXMLToSQLite
```
## Usage

```
./SoemXMLToSQLite -i inputPath -m sourceFileMask -d dbFilePath
```
#### inputPath 
This is the path that the files will be searched.
#### sourceFileMask 
This is filter that will be used to determine which files are going to be selected.
#### dbFilePath 
This is the path that the database will be created.


```
./SoemXMLToSQLite -i /home/ata/Downloads/inputFile -m *.xml -d soem.sqlite
```


If the input file path is found, the system asks which folder would you like to
select under the input file.
When you enter the name of the file, the filter applies and all of the files
under the file name you've entered gets saved in soem.sqlite.