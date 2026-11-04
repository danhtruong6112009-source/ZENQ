# Current editable SVG icon set

The app now loads assets/branding/editable/writer.svg, calc.svg, slides.svg, pdf.svg and creative.svg from disk first, with embedded SVG fallback. These are real editable vector paths and gradients, redrawn from the selected raster style. The PDF fold uses the same curved gradient as Slides and retains editable PDF text. Restart the app after edits to refresh cached icon images.

Download bundle: zenq-icons-editable.zip. Combined row: editable/zenq-icons.svg.

---
Previous design history (no longer loaded):

# Approved ZenQ application icons

The user selected the original v2 raster artwork. The shipped set reads Writer, Calc, Slides and Creative from zenq-app-icons-v2.png, and only PDF from zenq-app-icons-pdf-v5.png. Optical alpha bounds are normalized to equal display footprints. The later vector concepts are not loaded.

The PDF update was made with the built-in image_gen tool. Prompt: Edit this exact transparent five-icon atlas, replacing only the fourth red document icon's white linked-chain symbol with the exact uppercase word PDF in bold clean white sans-serif letters, centered in one line. Preserve the red gradient, folded corner, shape, lighting and placement. Preserve all other icons and the transparent background. The application uses the original atlas for the other four icons, ensuring their artwork remains unchanged.

---
Previous design iterations:

# ZenQ application icons v4

The shipped set is the five *-v4.svg files. Design: flowing ribbon volumes, three coordinated colour gradients, soft vector highlights, curved front panels, consistent silhouettes and large white symbols. Inspired by the depth and colour of Microsoft 365, with original ZenQ shapes. No background removal, raster fringe or clipped folds. The flat v3 set and the image-generated v2 atlas are retained only as rejected concepts; neither is loaded by the application.

# ZenQ application icons v3

The shipped icons are the five editable *-v3.svg files in this directory. They use flat layered rounded tiles, a restrained two-stop gradient, clear white symbols and a shared 212-pixel silhouette inside a 256-pixel square. No external shadows, background removal, perspective, or folded corners. AppIconRenderer rasterizes these embedded vectors once and caches each requested display size.

The initial v2 concept atlas was generated with the built-in image_gen tool. It was rejected during review in favor of this original vector design. The PNG is retained as a concept only and is not loaded by the application.

Initial generation prompt: Create one transparent horizontal atlas of five original ZenQ office-suite icons: blue document, emerald spreadsheet, orange slide/chart, coral PDF linked-page mark, violet creative pen nib. Modern Fluent design with folded ribbon panels, subtle depth, luminous gradients, no letters or brand logos. Equal optical size and margins.

Refinement prompt: Preserve the five designs and remove colored edge speckles and halos; make the PDF upper-right fold opaque rose pink and intact. The resulting raster concept was still rejected; no generated raster is shipped as the application icon set.
