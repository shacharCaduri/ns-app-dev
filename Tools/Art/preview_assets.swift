import AppKit
import Foundation
let root = URL(fileURLWithPath: FileManager.default.currentDirectoryPath)
let list = try JSONSerialization.jsonObject(with: Data(contentsOf: root.appendingPathComponent("Assets/Art/asset_inventory.json"))) as! [[String: Any]]
try FileManager.default.createDirectory(atPath: "tmp/art-review", withIntermediateDirectories: true)
for page in stride(from: 0, to: list.count, by: 40) {
 let width=1600,height=1100
 let out=NSBitmapImageRep(bitmapDataPlanes:nil,pixelsWide:width,pixelsHigh:height,bitsPerSample:8,samplesPerPixel:4,hasAlpha:true,isPlanar:false,colorSpaceName:.deviceRGB,bytesPerRow:width*4,bitsPerPixel:32)!
 NSGraphicsContext.saveGraphicsState()
 NSGraphicsContext.current=NSGraphicsContext(bitmapImageRep:out)
 NSColor(calibratedWhite:0.32,alpha:1).setFill();NSRect(x:0,y:0,width:width,height:height).fill()
 for (j,entry) in list[page..<min(page+40,list.count)].enumerated() {
  let path=entry["path"] as! String
  let img=NSImage(contentsOfFile:path)!
  let col=j%8,row=j/8,x=col*200,y=height-(row+1)*220
  NSColor(calibratedWhite:0.22,alpha:1).setFill();NSRect(x:x+2,y:y+2,width:196,height:216).fill()
  let scale=min(180/img.size.width,180/img.size.height,1)
  let w=img.size.width*scale,h=img.size.height*scale
  NSGraphicsContext.current?.imageInterpolation = .none
  img.draw(in:NSRect(x:Double(x)+100-w/2,y:Double(y)+35+(180-h)/2,width:w,height:h))
  let name=URL(fileURLWithPath:path).deletingPathExtension().lastPathComponent
  (name as NSString).draw(in:NSRect(x:x+6,y:y+4,width:188,height:30),withAttributes:[.font:NSFont.systemFont(ofSize:10),.foregroundColor:NSColor.white])
 }
 NSGraphicsContext.restoreGraphicsState()
 try out.representation(using:.png,properties:[:])!.write(to:root.appendingPathComponent("tmp/art-review/page_\(page/40+1).png"))
}
