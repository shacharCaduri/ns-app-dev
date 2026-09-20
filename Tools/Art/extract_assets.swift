import Foundation
import AppKit

// Run from the repository root: swift Tools/Art/extract_assets.swift
struct Entry: Codable {
    let source: String
    let output: String
    let rect: [Int]
    let mirror: Bool?
    let canvas: [Int]?
    let polygon: [[Int]]?
    let exclude: [[Int]]?
    let padding: Int?
    let anchorX: Int?
}
let root = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
let entries = try JSONDecoder().decode([Entry].self, from: Data(contentsOf: root.appendingPathComponent("Tools/Art/assets.json")))
var sources: [String: NSBitmapImageRep] = [:]
var inventory: [[String: Any]] = []
for entry in entries {
    if sources[entry.source] == nil {
        let data = try Data(contentsOf: root.appendingPathComponent(entry.source))
        guard let bitmap = NSBitmapImageRep(data: data) else { fatalError("Cannot decode \(entry.source)") }
        sources[entry.source] = bitmap
    }
    let source = sources[entry.source]!
    let r = entry.rect
    // Rectangles use coordinates of the original reference sheet.
    let referenceWidth = entry.source.contains("arena-objects") ? 1451 : entry.source.contains("portal") ? 1448 : 1254
    let referenceHeight = entry.source.contains("arena-objects") ? 1084 : entry.source.contains("portal") ? 1086 : 1254
    let sx = Double(source.pixelsWide) / Double(referenceWidth)
    let sy = Double(source.pixelsHigh) / Double(referenceHeight)
    let width = r[2] - r[0], height = r[3] - r[1]
    var pixels = [UInt8](repeating: 0, count: width * height * 4)
    var minX = width, minY = height, maxX = -1, maxY = -1
    for y in 0..<height { for x in 0..<width {
        let gx = r[0]+x, gy = r[1]+y
        if let regions = entry.exclude, regions.contains(where: { gx >= $0[0] && gx < $0[2] && gy >= $0[1] && gy < $0[3] }) { continue }
        if let polygon = entry.polygon {
            let shape = NSBezierPath()
            shape.move(to: NSPoint(x: polygon[0][0], y: polygon[0][1]))
            for point in polygon.dropFirst() { shape.line(to: NSPoint(x: point[0], y: point[1])) }
            shape.close()
            if !shape.contains(NSPoint(x: gx, y: gy)) { continue }
        }
        let px = min(source.pixelsWide - 1, Int((Double(r[0] + x) + 0.5) * sx))
        let py = min(source.pixelsHigh - 1, Int((Double(r[1] + y) + 0.5) * sy))
        guard let color = source.colorAt(x: px, y: py)?.usingColorSpace(.deviceRGB) else { continue }
        let a = Int((color.alphaComponent * 255).rounded())
        // Existing sheets contain almost invisible export noise in their alpha.
        if a < 24 { continue }
        let i = (y * width + x) * 4
        pixels[i] = UInt8(clamping: Int((color.redComponent * 255).rounded()))
        pixels[i+1] = UInt8(clamping: Int((color.greenComponent * 255).rounded()))
        pixels[i+2] = UInt8(clamping: Int((color.blueComponent * 255).rounded()))
        pixels[i+3] = UInt8(clamping: a)
        minX = min(minX, x); minY = min(minY, y); maxX = max(maxX, x); maxY = max(maxY, y)
    }}
    guard maxX >= minX && maxY >= minY else { fatalError("Empty asset: \(entry.output)") }
    let trimW = maxX-minX+1, trimH = maxY-minY+1
    let padding = entry.padding ?? 4
    let outW = max(entry.canvas?[0] ?? 0, trimW+padding*2)
    let outH = max(entry.canvas?[1] ?? 0, trimH+padding*2)
    let offsetX = entry.anchorX.map { outW/2 - ($0-r[0]-minX) } ?? (outW-trimW)/2
    let offsetY = outH-trimH-padding
    precondition(offsetX >= 0 && offsetX+trimW <= outW, "Canvas too narrow: \(entry.output)")
    let out = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: outW, pixelsHigh: outH, bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false, colorSpaceName: .deviceRGB, bitmapFormat: .alphaNonpremultiplied, bytesPerRow: outW*4, bitsPerPixel: 32)!
    memset(out.bitmapData!, 0, outW*outH*4)
    for y in minY...maxY { for x in minX...maxX {
        let tx = entry.mirror == true ? outW-1-(offsetX+x-minX) : offsetX+x-minX
        let ti = (offsetY+y-minY)*out.bytesPerRow+tx*4
        let si = (y*width+x)*4
        for c in 0..<4 { out.bitmapData![ti+c] = pixels[si+c] }
    }}
    let url = root.appendingPathComponent(entry.output)
    try FileManager.default.createDirectory(at: url.deletingLastPathComponent(), withIntermediateDirectories: true)
    try out.representation(using: .png, properties: [:])!.write(to: url)
    inventory.append(["path": entry.output, "source": entry.source, "sourceRect": r, "width": outW, "height": outH, "mirrored": entry.mirror ?? false, "pivot": [0.5, Double(padding)/Double(outH)]])
}
let data = try JSONSerialization.data(withJSONObject: inventory, options: [.prettyPrinted, .sortedKeys])
try data.write(to: root.appendingPathComponent("Assets/Art/asset_inventory.json"))
print("Extracted \(inventory.count) transparent assets.")
