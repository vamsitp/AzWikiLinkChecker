### azWikiLinkChecker

> _**pre-req**: install [`dotnet 5.0 SDK`](https://dotnet.microsoft.com/download/dotnet/5.0) (if not already installed)_   
**`dotnet tool install -g --ignore-failed-sources AzWikiLinkChecker`**   

**USAGE**
> **`azwlc -o "AzDO Org" -p "AzDO Project" -w "AzDO Wiki" -t "PAT"`**   
> CSV _output_ is saved to user's _Desktop_ folder with the name `azwlc_***.csv`

- `.attachments` & `anchors (#)` are in <font color="gold">dark-yellow</font>   
- External URLs are in <font color="blue">blue</font>   
- Internal _link-issues_ are in <font color="red">red</font>   