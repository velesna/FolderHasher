using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using Microsoft.SqlServer.Server;

//USE Northwind;
//exec sp_configure 'show advanced options', 1
//exec sp_configure 'clr_enabled', 1
//exec sp_configure 'clr strict security', 0
//RECONFIGURE
//GO

//DROP FUNCTION BuildHash
//DROP ASSEMBLY Hasher
//GO

//CREATE ASSEMBLY Hasher
//FROM 'B:\WebTutorClient\StoredProcedure\StoredPocedure\bin\Release\Hasher.dll'
//WITH PERMISSION_SET = UNSAFE--EXTERNAL_ACCESS
//GO

//CREATE FUNCTION BuildHash(@Path nvarchar(max)) RETURNS nvarchar(max) AS EXTERNAL NAME Hasher.FolderHasher.BuildHash

//* GO
//* CREATE PROCEDURE FolderHash  // создаём конкретную хранимую процедуру на основе сборки
//* AS EXTERNAL NAME assembly_name.class_name.method_name
//* 
//* GO
//* CREATE ASSEMBLY assembly_name [ AUTHORIZATION owner_name ]
//* FROM {dll_file}
//* [WITH PERMISSION_SET = { SAFE | EXTERNAL_ACCESS | UNSAFE }]


public static class FolderHasher
{
    [SqlFunction]
    public static SqlString BuildHash(SqlString path)
    {
        byte[] buffer = new byte[4096];

        using (var MD5 = new MD5CryptoServiceProvider())
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path.Value);
                var FileList = di.EnumerateFiles("*", SearchOption.AllDirectories).ToList();

                FileList.Sort((x, y) =>
                {
                    if (x == null && y == null) return 0;
                    else if ((x == null) || (x.Length < y.Length)) return -1;
                    else if ((y == null) || (x.Length > y.Length)) return 1;
                    else return x.Name.CompareTo(y.Name);
                });

                foreach (var file in FileList)
                {
                    int length;
                    using (var fs = File.OpenRead(file.FullName))
                    {
                        do
                        {
                            length = fs.Read(buffer, 0, buffer.Length);
                            MD5.TransformBlock(buffer, 0, length, buffer, 0);
                        }
                        while (length > 0);
                    }
                    Console.WriteLine(file);
                }
                MD5.TransformFinalBlock(buffer, 0, 0);
            }
            catch (DirectoryNotFoundException)
            {
                return (SqlString)"Path not found"; //dnf.ToString();
            }
            catch (Exception)
            {
                return SqlString.Null; // String.Empty;
            }
            
            return (SqlString)BitConverter.ToString(MD5.Hash).Replace("-", String.Empty);
        }
    }
}
