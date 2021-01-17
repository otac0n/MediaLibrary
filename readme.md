MediaLibrary
=======

[![MIT Licensed](https://img.shields.io/badge/license-MIT-blue.svg?style=flat-square)](license.md)

Getting Started
---------------

To build, you will need the following prerequisites:
* Node.js
* Angular CLI

To run the app, you will need to add an ACL entry to allow the web hosting component:

    netsh http add urlacl url=http://+:9000/ user=%USERDOMAIN%\%USERNAME%

Drag and drop folders onto the app, and it will index them.  The search box at the top has a dialect similar to Google search.
