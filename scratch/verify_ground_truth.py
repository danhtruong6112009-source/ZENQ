import os
import sys
import json

manifest_path = "tests/ground_truth_manifest.json"
gt_dir = "tests/ground_truth"
samples_dir = "tests/samples"

if not os.path.exists(manifest_path):
    print("FAIL: Manifest not found")
    sys.exit(1)

with open(manifest_path, "r", encoding="utf-8") as f:
    manifest = json.load(f)

print("=== Ground Truth Manifest Verification ===")
print("Schema Version:", manifest.get("schema_version"))
stats = manifest.get("statistics", {})
for k, v in stats.items():
    print(f"  {k}: {v}")

missing_samples = []
missing_parts = []
total_parts = 0
circle_nodes = []
non_circle_nodes = []

for rel_path, sample_info in manifest.get("samples", {}).items():
    sample_file = os.path.join(samples_dir, rel_path)
    if not os.path.exists(sample_file):
        missing_samples.append(sample_file)
    
    category = sample_info.get("category")
    filename = sample_info.get("filename")
    base_name = os.path.splitext(filename)[0]
    unpacked_folder = os.path.join(gt_dir, category, base_name)
    
    if not os.path.exists(unpacked_folder):
        missing_parts.append(unpacked_folder)
    else:
        for part in sample_info.get("unpacked_parts", []):
            p = os.path.join(unpacked_folder, part)
            if not os.path.exists(p):
                missing_parts.append(p)
            else:
                total_parts += 1
                
    # Check smartart nodes
    sa = sample_info.get("smartart")
    if sa and "nodes" in sa:
        for n in sa["nodes"]:
            geom = n.get("geometry")
            ar = n.get("aspect_ratio", 0)
            is_circle = n.get("is_circle_1_to_1", False)
            if geom in ["ellipse", "circle"] or is_circle:
                circle_nodes.append((rel_path, n.get("model_id"), ar, is_circle, geom))
            else:
                non_circle_nodes.append((rel_path, n.get("model_id"), ar, geom))

print("\n--- Disk Integrity Check ---")
print(f"Total DOCX Sample Files checked: {len(manifest.get('samples', {}))}")
print(f"Missing DOCX samples: {len(missing_samples)}")
if missing_samples:
    for m in missing_samples[:5]:
        print(f"  Missing: {m}")

print(f"Total Unpacked XML Parts checked: {total_parts}")
print(f"Missing Unpacked Parts: {len(missing_parts)}")
if missing_parts:
    for m in missing_parts[:5]:
        print(f"  Missing part: {m}")

print("\n--- SmartArt Circle Geometry in Ground Truth ---")
print(f"Total Circle/Cycle Nodes in Ground Truth: {len(circle_nodes)}")
perfect_circles = 0
for path, mid, ar, is_circ, geom in circle_nodes:
    diff = abs(ar - 1.0)
    if diff <= 0.005:
        perfect_circles += 1
    else:
        print(f"  [DEVIATION] {path} node {mid}: AR={ar}, geom={geom}")

print(f"Nodes with Aspect Ratio 1.000 ± 0.005: {perfect_circles} / {len(circle_nodes)} ({100.0 * perfect_circles / max(1, len(circle_nodes)):.1f}%)")

print("\n--- SmartArt Folders Overview ---")
sa_folders = os.listdir(os.path.join(gt_dir, "smartart"))
print(f"Total SmartArt folders in tests/ground_truth/smartart: {len(sa_folders)}")
suites_folders = os.listdir(os.path.join(gt_dir, "suites"))
print(f"Total Suites folders in tests/ground_truth/suites: {len(suites_folders)}")
scenarios_folders = os.listdir(os.path.join(gt_dir, "scenarios"))
print(f"Total Scenarios folders in tests/ground_truth/scenarios: {len(scenarios_folders)}")

all_ok = (len(missing_samples) == 0 and len(missing_parts) == 0 and perfect_circles == len(circle_nodes))
print("\nManifest Ground Truth Verification:", "PASSED" if all_ok else "FAILED")
