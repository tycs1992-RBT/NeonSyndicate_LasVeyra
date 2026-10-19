# World topology and source measurements

Accepted starting source is latest04N `cd6d684541f86a4b1a31a8bfc8335783b9f99e2f`, one descendant of supplied `bbb73a592caeff9603e6ce468854e1dc16a3a11e`. The newer owner commit adds shader materials and a recovery scene; those files remain intact. The04O branch did not exist when execution began.

CityLayout.Generate remains the original seeded generator. GameRoot first selects the twelve inhabited venues, then explicitly applies the new profile before Underheart binding, map rasterization and city construction. No CityLayout CoreHalf, RingHalf, MapHalf, canal, road anchor or seed expression was scaled or changed. Original POI/site/collectible/parking coordinates and lane graph are retained. Profile revision is part of the existing CityLayout, not a second runtime world manager.

Protected service lots, named/special parcels, helipads and antenna roofs remain anchored. Ordinary parcels are split around a mineral court. Outside bounded service/settlement hubs they become small low ruins. Receipt-bearing ordinary roofs retain exact landing dimensions on framed structures. New volume data drives both render meshes and matching box or exact faceted mesh colliders. Conservative building-footprint spawn exclusions remain in force.

At seed61724:562 ordinary lots rebuilt;33 admitted ground passages; four Spine crowns and three mineral links. Total building-record footprint falls from1,002,458.75 to716,522.94 square metres,28.52%. This is a parcel footprint measurement, not a40-50% reduction of the entire city region, nor measured player travel time. WORLD_MANIFEST.json is authoritative for exact floating-point output.

The existing directed vehicle graph is unchanged. The pedestrian graph gains sampled solid/prop-safe six-metre ground links and actual pavement. Existing crossing signal links remain. On-foot preview GPS tries the pedestrian graph; other travel retains original routing. No elevated path is implied by decorative crowns. See TRAVERSAL_MATRIX.md for measured service paths and unresolved endpoints.

SPATIAL_REFERENCES.json inventories inspected fixed-position source expressions; WORLD_MANIFEST.json inventories all generated POIs/sites, selected venue approaches and routes. They are not a verified semantic migration table for all past save coordinates.
