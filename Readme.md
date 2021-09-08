### azWikiLinkChecker

> _**pre-req**: install [`dotnet 5.0 SDK`](https://dotnet.microsoft.com/download/dotnet/5.0) (if not already installed)_   
**`dotnet tool install -g --ignore-failed-sources AzWikiLinkChecker`**   

> _PAT_ needs to have wiki Read/Write permission in place. It doesn't make any changes to the Wiki content, but some of the API requests are POSTs and require those permissions.

**USAGE**
> **`azwlc -o "AzDO Org name" -p "AzDO Project name" <opitonal: -w "AzDO Wiki" -v "branch_name"> -t "PAT"`**  
  > All parameters should be plain text (not URL encoded)   
> CSV _output_ is saved to user's _Desktop_ folder with the name `azwlc_***.csv`   
> Sample [azure-pipeline](./azwlc_pipeline.yml)

- `.attachments` & `anchors (#)` are in <font color="gold">dark-yellow</font>   
- External URLs are in <font color="blue">blue</font>   
- Internal _link-issues_ are in <font color="red">red</font>   
