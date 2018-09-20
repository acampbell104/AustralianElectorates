

## Electorate information

All information about electorates is available at [/Data/electorates.json](https://github.com/SimonCropp/AustralianElectorates/blob/master/Data/electorates.json)


### Example content

```
[
  {
    "Name":"Canberra",
    "State":"ACT",
    "Description":"<p>The Division of Canberra covers an area in central ACT consisting of the Districts of:</p><ul><li>Canberra Central,</li><li>Kowen,</li><li>Majura,</li><li>part of Belconnen,</li><li>part of Jerrabomberra,</li><li>part of Molonglo Valley,</li><li>part of Weston Creek, and</li><li>part of Woden Valley</li></ul>",
    "Area":312.0,
    "ProductsAndIndustry":"Mainly residential with tourism, retail and some light industry at Fyshwick and Beard",
    "NameDerivation":"A locality name derived from an Aboriginal word which is held to mean 'meeting place'.",
    "DateGazetted":"2018-07-13T00:00:00",
    "Members":[
      {
        "Name":"Brodtmann, G",
        "Begin":2010,
        "Party":"ALP"
      },
      {
        "Name":"Ellis, A",
        "Begin":1998,
        "End":2010,
        "Party":"ALP"
      }
    ],
    "DemographicRating":"<strong>Inner Metropolitan</strong> - situated in capital cities and consisting of well-established built-up suburbs"
  },
```


## Maps

All maps are in [geojson format](http://geojson.org/).

The following grouping of maps exist:

 * Future (Next election) [/Data/Maps/Future](/Data/Maps/Future)
 * Current (2016 election) [/Data/Maps/Current](/Data/Maps/Current)


### Structure

Each of the above groupings have the following structure.

 * Australia and state "combined electorate" maps are at the root.
 * Specific electorate maps are located inside a subdirectory based on the state they exist in.


### Map variants

Each maps has multiple variants based on simplification.

With the two options combined, there are 5 different options for each map.

Below is the combinations for [Bass](https://www.aec.gov.au/profiles/tas/bass.htm)

| Size  | File name              | Simplification |
| -----:| ---------------------- | --------------:|
| 2.8MB | [bass.geojson](/Data/Maps/Future/Electorates/bass.geojson)       | none |
| 231KB | [bass_20.geojson](/Data/Maps/Future/Electorates/bass_20.geojson) | 20%  |
| 94KB  | [bass_10.geojson](/Data/Maps/Future/Electorates/bass_10.geojson) | 20%  |
| 46KB  | [bass_05.geojson](/Data/Maps/Future/Electorates/bass_05.geojson) | 5%   |
| 8KB   | [bass_01.geojson](/Data/Maps/Future/Electorates/bass_01.geojson) | 1%   |


#### Simplification

Simplication uses [MapShaper simplify option](https://github.com/mbloch/mapshaper/wiki/Command-Reference#-simplify)

> Visvalingam simplification iteratively removes the least important point from a polyline. The importance of points is measured using a metric based on the geometry of the triangle formed by each non-endpoint vertex and the two neighboring vertices

The level of simplification is represented as a percent number. 20, 10, 5, and 1. representing 20%, 10%, 5%, and 1%. The smaller the number the smaller the file, but with the loss of some accuracy.


## Copyright

The code in this repository is licensed under MIT.

The content that is rendered (all files under [/Data/](/Data/)) is sourced from the [Australian Electoral Commission (AEC)](https://www.aec.gov.au/) and remains under the [AEC Copyright](https://www.aec.gov.au/footer/Copyright.htm).


## Re-Generating the data

Some tools are required for GIS operations.


### ogr2ogr

Part of [Geospatial Data Abstraction Library (GDAL)](https://www.gdal.org/)

 * Download https://trac.osgeo.org/gdal/wiki/DownloadingGdalBinaries


### MapShaper

[MapShaper](https://github.com/mbloch/mapshaper/)

Installation:

* Install [Node](https://nodejs.org/)
* [Install MapShaper globally](https://github.com/mbloch/mapshaper#installation) `npm install -g mapshaper`

https://github.com/mbloch/mapshaper/wiki/Command-Reference