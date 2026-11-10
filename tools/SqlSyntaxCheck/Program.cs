using Microsoft.SqlServer.TransactSql.ScriptDom;
var parser=new TSql160Parser(true);var count=0;
foreach(var path in Directory.GetFiles(args[0],"*.sql")) {
 using var reader=File.OpenText(path);parser.Parse(reader,out var errors);
 foreach(var error in errors) {Console.WriteLine($"FAIL {Path.GetFileName(path)}:{error.Line}:{error.Column} {error.Message}");count++;}
 if(errors.Count==0)Console.WriteLine($"PASS SQL Server 2022 syntax: {Path.GetFileName(path)}");
}
return count==0?0:1;
