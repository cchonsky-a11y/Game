# The art pack

Roman street architecture, props and (next) people for AD 155, made for this game. Everything is modelled by script in
Blender and exported as one glTF file the game loads. No downloaded or licensed art: the pack is the project's own.

## How it is made
- `art/kit/textures.py` paints the materials: tileable, physically based maps (colour, normal, roughness) at real scale.
- `art/kit/build_kit.py` models each asset (1 unit = 1 metre, fronts face the street) and exports `kit.glb`.
- Build: install Blender's Python module (`pip install bpy numpy` in a virtual environment), then
  `python art/kit/build_kit.py art/out/kit.glb` and copy the result to `src/Butterfly.Web/wwwroot/kit.glb`.
- The game loads the file once and sets copies down (`city.js`: `loadKit`, `City.kit`). A material named `plaster#e8c79a`
  is the painted plaster multiplied by that colour, so one texture serves every wall colour.

## What is in it (tranche 1, 2026-11-11)
| Asset | What it is | Historical basis (to be checked against sources before release) |
|---|---|---|
| `insula_a` … `insula_f` | Apartment blocks of 2–5 storeys | The multi-storey insulae of Rome and Ostia: shops (tabernae) open to the street between brick piers, a timber lintel, a mezzanine with a small window over each shop, a masonry counter with jars set into it, shutter boards stacked to one side; plastered flats above with shuttered windows and timber balconies (maeniana) on brackets; low tiled roofs |
| `temple` | A temple on a high podium | Roman, not Greek, form: steps only at the front between cheek walls, a deep porch of Corinthian columns, a closed cella, a pediment with dentils and roof ornaments, bronze doors |
| `column_tuscan`, `column_corinthian` | Free-standing columns | The plain Tuscan order and the fluted Corinthian with two rings of acanthus |
| `street_fountain` | A public street fountain | The trough fountains of Pompeii: four upright stone slabs clamped with lead, a standing stone carved with a spout |
| `amphora_oil`, `amphora_wine` | Transport jars | The round Spanish oil amphora (Dressel 20) and the tall wine amphora with a spike foot (Dressel 2–4) |

Materials: lime plaster, thin Roman brick with thick mortar, travertine ashlar, white marble, polygonal basalt street
paving, roof tiles (flat pans with half-round covers), sawn timber, fired clay, bronze with verdigris.

## Still drawn by code in the game (to be replaced by pack assets)
The Senate house, the spiral column, the rostra, the shrine, the street bar, the market stalls and pavilion, the forges'
fittings, the mill wheel, carts, braziers, trees, the skyline, the machine, and every person.

## Next tranches
1. People: a rigged figure with walk and idle animation, dressed by station (tunic, toga, stola and palla), with variants.
2. The remaining street furniture and the named buildings above.
3. Streets: basalt paving with kerbs and stepping stones; plaza paving.
4. The city after the jump: a weathered and altered kit (the same streets a generation or two later).
5. A kit per later era.

## Honest limits
This is procedural modelling: consistent, historically grounded and light enough for a phone, but it is not sculpted by
hand. Faces, cloth and ornament are simplified. A human artist could later replace any asset one for one: the game only
needs the same names in `kit.glb`.
