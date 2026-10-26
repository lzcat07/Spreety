# Artwork provenance

Generated with the built-in imagegen tool. No API-key fallback was used. Final asset: `assets/characters.png`.

Initial prompt:

> Use case: stylized-concept. Create a production sprite atlas PNG with genuine transparent background for tiny Windows desktop companions. Exactly 3 equal columns and 2 equal rows, each cell isolated with generous padding and nothing crosses cells. Columns: Monkey D. Luffy from One Piece (straw hat, black hair, red vest, blue shorts); Senku Ishigami from Dr Stone (tall pale green spiky hair with dark green tips, cream science tunic); Rei Ayanami from Evangelion (short pale blue hair, red eyes, white plugsuit with black and orange accents). Top row: each standing front view, hands relaxed. Bottom row: same three seated front view, tiny legs dangling forward. Cute premium vinyl funko-like chibi proportions, large heads, tiny bodies, soft 3D shading, clean crisp silhouettes readable at 72px height. Consistent character scale and alignment, full body including hat and hair entirely inside each cell. No words, no floor, no background, no shadows outside characters, no cell borders. Intended as 3 by 2 evenly spaced atlas.

Final edit prompt, using the first image as the target:

> Background extraction edit. Remove ALL the colored gradient background around all six figures completely. Output actual PNG alpha transparency, not black, not checkerboard, not gradient. Preserve exactly the same 3 columns 2 rows and all six full body characters and their poses and positions. Make characters a little smaller if necessary so each is entirely inside its exact equal 512 by 512 cell in the 1536x1024 atlas. Top-row figures currently cross the center at y512: scale all top row to fit top half with 30px transparent margins. The output must be truly transparent outside the six individual character silhouettes. No shadows, no glow, no floor.

Both tool calls set `transparent_background=true`. The actual final atlas needed a 55% row split and a narrow Senku-column inset, documented in ARCHITECTURE.md. Automated tests verify transparent pixels in every final sprite crop.
