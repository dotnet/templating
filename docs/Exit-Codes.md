# `dotnet new` exit codes and their meaning

Exit codes are chosen to confrom to existing standards or standardization attempts and well known exit code. See [Related resources](#related) for more details 

| Exit&nbsp;Code | Reason |
|:-----|----------|
| 0 | Success |
| [70](#70) | Unexpected internal software issue. |
| [73](#73) | Can't create output file. |
| [100](#100) | Instantiation Failed - Processing issues. |
| [101](#101) | Instantiation Failed - Missing mandatory parameter(s) for template. |
| [102](#102) | Instantiation/Search Failed - parameter(s) value(s) invalid. |
| [103](#103) | The template was not found. |
| [104](#104) | The operation was cancelled. |
| [105](#105) | Instantiation Failed - Post action failed. |
| [106](#106) | Installation/Uninstallation Failed. |
| [107 - 113](#107) | Reserved |

## <a name="70"></a>70 - Unexpected internal software issue

The result received from template engine core is not expected. [File a bug](https://github.com/dotnet/templating/issues/new?title=Unexpected%20Internal%20Software%20Issue%20(EX_SOFTWARE)) if you encounter this exit code.

This is a semi-standardized exit code (see [EX_SOFTWARE in /usr/include/sysexits.h](https://github.com/openbsd/src/blob/master/include/sysexits.h#L107))


## <a name="73"></a>73 - Can't create output file.

The operation was cancelled due to detection of an attempt to perform destructive changes to existing files. This can happen if you are attempting to instantiate template into the same folder where it was previously instantiated under same target name (specified via `--name` option or defaults to the target directory name)

_Example:_
```console
> dotnet new console

The template "Console App" was created successfully.

Processing post-creation actions...
Running 'dotnet restore' on C:\tmp\tmp.csproj...
  Determining projects to restore...
  Restored C:\tmp\tmp.csproj (in 47 ms).
Restore succeeded.

> dotnet new console

Creating this template will make changes to existing files:
  Overwrite   ./tmp.csproj
  Overwrite   ./Program.cs

Rerun the command and pass --force to accept and create.

For details on current exit code please visit https://aka.ms/templating-exit-codes#73
```

Destructive changes can be forced by passing `--force` option

This is a semi-standardized exit code (see [EX_CANTCREAT in /usr/include/sysexits.h](https://github.com/openbsd/src/blob/master/include/sysexits.h#L110))


## <a name="100"></a>100 - Instantiation Failed - Processing issues

The template instantiation failed due to error(s). Caused either by environment (failure to read/write template(s) or cache) or by errorneous template(s) (incomplete conditions, symbols or macros etc.). Exact error reason will be output to stderr.

_Examples:_

Missing mandatory properties in template.json
```json
{
    "author": "John Doe",
    "name": "name",
}
```

Incomplete condition in template file:

```C#
// #if( MySwitch == "DefineFunc"
    static void Foo() { } 
```

## <a name="101"></a>101 - Instantiation Failed - Missing mandatory parameter(s) for template.

A parameter [marked as required](Reference-for-template.json.md#isrequired) was not supplied during template instantiation.

_Example:_
```console
> dotnet new my-template
Mandatory option --MyMandatoryParam missing for template My Template.

For details on current exit code please visit https://aka.ms/templating-exit-codes#101
```

## <a name="102"></a>102 - Instantiation/Search Failed - parameter(s) value(s) invalid.

Usually a mismatch in type of the specified parameter or unrecognized choice option. Applicable to `search` command with not enough information as well.

_Examples:_
```console
> dotnet new console --framework xyz
Error: Invalid option(s):
--framework xyz
   'xyz' is not a valid value for --framework. The possible values are:
      net6.0   - Target net6.0
      net7.0   - Target net7.0

For details on current exit code please visit https://aka.ms/templating-exit-codes#102
```

```console
> dotnet new search
Search failed: not enough information specified for search.
To search for templates, specify partial template name or use one of the supported filters: '--author', '--baseline', '--language', '--type', '--tag', '--package'.
Examples:
   dotnet new search web
   dotnet new search --author Microsoft
   dotnet new search web --language C#

For details on current exit code please visit https://aka.ms/templating-exit-codes#102
```

## <a name="103"></a>103 - The template was not found.

Applicable to instantiation, listing and remote sources searching.

_Examples:_
```console
> dotnet new xyz
No templates found matching: 'xyz'.

To list installed templates, run:
   dotnet new list
To search for the templates on NuGet.org, run:
   dotnet new search xyz

For details on current exit code please visit https://aka.ms/templating-exit-codes#103
```

```console
> dotnet new list xyz
No templates found matching: 'xyz'.

To search for the templates on NuGet.org, run:
   dotnet new search xyz

For details on current exit code please visit https://aka.ms/templating-exit-codes#103
```

```console
> dotnet new search xyz
Searching for the templates...
Matches from template source: NuGet.org
No templates found matching: 'xyz'.

For details on current exit code please visit https://aka.ms/templating-exit-codes#103
```

## <a name="104"></a>104 - The operation was cancelled. 

Currently applicable only to case when user aborts custom post action.


## <a name="105"></a>105 - Instantiation Failed - Post action failed.

## <a name="106"></a>106 - Installation/Uninstallation Failed

Failure to download packages, read/write templates or cache, erorrneous or corrupted template, or an attempt to install same package multiple times.

_Example:_
```console
> dotnet new install foobarbaz
The following template packages will be installed:
   foobarbaz

foobarbaz could not be installed, the package does not exist.

For details on current exit code please visit https://aka.ms/templating-exit-codes#106
```

## <a name="107"></a><a name="108"></a><a name="109"></a><a name="110"></a><a name="111"></a><a name="112"></a><a name="113"></a>107 - 113

Reserved for future use.

[File a bug](https://github.com/dotnet/templating/issues/new?title=Unexpected%20Exit%20Code) if you encounter any of these exit codes.


<BR/>
<BR/>
<BR/>

### Related Resources
* [`BSD sysexit.h`](https://github.com/openbsd/src/blob/master/include/sysexits.h)
* [`Special exit codes - Lynux documentation project`](https://tldp.org/LDP/abs/html/exitcodes.html)
