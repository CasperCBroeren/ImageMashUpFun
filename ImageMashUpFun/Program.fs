open FSharp.Data 
open System
open Microsoft.FSharp.Control.CommonExtensions 
open System.IO
open System.Drawing
open System.Threading.Tasks

[<Literal>]
let sourceUrl = @"https://www.vicompany.nl/vi-company"
let saveToPath = ".\\images"

module Imaging = 
    let colorMax (color1:Color, color2:Color) = 
        Color.FromArgb(int color1.A,int (max color1.R color2.R),int (max color1.G color2.G),int (max color1.B color2.B))

    let colorMin(color1:Color, color2:Color) = 
        Color.FromArgb(int color1.A,int (min color1.R color2.R),int (min color1.G color2.G),int (min color1.B color2.B))

    let mixTwoImages (imageA : string option, imageB:string option) = 
        match imageA,imageB  with         
        | Some aValue, Some bValue  ->
            printfn "Mix made %s, %s" aValue bValue
            let aImage = new Bitmap(aValue)
            let bImage = new Bitmap(bValue)
            let w, h = aImage.Width, aImage.Height
            let target = new Bitmap(w, h)
            for x in 0 .. (w-1) do
                for y in 0 .. (h-1) do
                    let origin = aImage.GetPixel(x,y)
                    let noise  = bImage.GetPixel(x,y)
                    let result = colorMin( origin, noise)
                    target.SetPixel(x,y, result)
            let resultPath = aValue.Replace(".jpg", String.Empty)+"_mix.jpg"
            printfn "Result at '%s'" resultPath
            target.Save(resultPath)
        | _,_ -> printfn "Empty a or b"

module Web =
    open System.Net

    type ViCoTeam = HtmlProvider<sourceUrl>
  
    let downloadImage(url:string , fileName:string) =            
        async {
              let destinationPath = Path.Combine(saveToPath, fileName + ".jpg")
              let req = WebRequest.Create(Uri(url)) 
              use! resp = req.AsyncGetResponse()  
              use stream = resp.GetResponseStream()  
              use ms = new MemoryStream()
              stream.CopyTo(ms)
              File.Delete(destinationPath)
              File.WriteAllBytes(destinationPath, ms.ToArray())
           
              return Some destinationPath 
        }  
        
 
    let getData() = 
        let data = ViCoTeam.GetSample()
        data.Html.CssSelect("div#people  div.grid  div.cell img.media")
        |> List.map (fun item -> item.AttributeValue("alt"), item.AttributeValue("src"))
        |> List.sortBy (fun (name, image) -> name )     

[<EntryPoint>]
let main argv = 
    let colleagues = Web.getData()
    let sun = Some "sun.jpg"
    Directory.CreateDirectory(saveToPath) |> ignore
    let images() = colleagues
                    |> List.map(fun(name, url) -> Web.downloadImage(url, name))
                    |> Async.Parallel
                    |> Async.RunSynchronously   
    images()
        |> Array.iter(fun item -> Imaging.mixTwoImages(item, sun))
    Console.ReadKey().KeyChar |> int 
   
