# Chunked file upload
[![License Apache2](https://img.shields.io/hexpm/l/plug.svg)](http://www.apache.org/licenses/LICENSE-2.0)

Chunked file uploading to a .NET Core 3.1 API server.
The client uses [Resumable.js](http://www.resumablejs.com/), which in turn uses HTML 5 file API to generate the chunks.

## How to run

Install requirements:

[.Net Core](https://www.microsoft.com/net/download)

Run:

    cd Arad.ChunkedUpload
    open Arad.ChunkedUpload.sln via visual studio 2019

Then browse to [localhost:5000](http://localhost:5000).
API Documentation will be available on [localhost:5000/api-docs](http://localhost:5000/api-docs).


Upload generated files, download it from server and check whether the checksums match with ``sha1sum``

    sha1sum file.tmp

