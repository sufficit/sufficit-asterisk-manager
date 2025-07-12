# Copilot Instructions
<!-- Version: 3.5 -->

## Common Guidelines
* whenever you find a newer Copilot Instructions file, merge the Common Guidelines content and adjust the version;
* code comments should always be in English;
* response to user queries should be in IDE current language;
* avoid to change code that was not related to the query;
* when agent has to change a method and it change the async status, the agent should update the method callers too;
* for extensions methods use always "source" as default parameter name
* use one file for each class
* for #region tags: no blank lines between consecutive regions, but always add one blank line after region opening and one blank line before region closing
* do not try to build if you just changed the code comments or documentation files;

## Documentation Guidelines
* for github repository readme.md files, use the following structure:
  - Title
  - About (description, etc.)
  - Features
  - Installation
  - Usage (always on separated readme file, one for each case; testing, etc.)
  - License
  - Support (development@sufficit.com.br, etc.)
  - Related (references, thanks, etc.)

## Project Specific Guidelines
* Pay attention to Action correct names, it is case sensitive;
* Pay attention to Properties required or optional;

### Asterisk Documentation
* Sufficit.Asterisk.Shared -> asterisk-documentation-resume.md
* https://docs.asterisk.org
* https://github.com/asterisk/asterisk
* https://github.com/asterisk/documentation
