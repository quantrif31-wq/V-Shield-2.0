param([Parameter(Mandatory=$true)][string]$Docx,[Parameter(Mandatory=$true)][string]$Pdf)
$word=$null; $doc=$null
try {
  $word=New-Object -ComObject Word.Application
  $word.Visible=$false
  $word.DisplayAlerts=0
  $doc=$word.Documents.Open($Docx,$false,$false)
  foreach($t in $doc.Tables){
    if($t.Cell(1,1).Range.Text.Trim([char]13,[char]7) -eq 'Thực thể'){
      $t.AllowAutoFit=$false
      $t.Columns.Item(1).Width=135
      $t.Columns.Item(2).Width=90
      $t.Columns.Item(3).Width=55
      $t.Columns.Item(4).Width=100
      $t.Columns.Item(5).Width=55
      $t.Columns.Item(1).Range.Font.Name='Arial Narrow'
      $t.Columns.Item(1).Range.Font.Size=8
      for($ri=2;$ri -le $t.Rows.Count;$ri++){$t.Cell($ri,1).FitText=$true}
    }
  }
  foreach($toc in $doc.TablesOfContents){$toc.Update()}
  foreach($story in $doc.StoryRanges){$range=$story; while($null -ne $range){$range.Fields.Update() | Out-Null; $range=$range.NextStoryRange}}
  $doc.Repaginate()
  $doc.Save()
  $doc.ExportAsFixedFormat($Pdf,17)
} finally {
  if($doc){$doc.Close($false)}
  if($word){$word.Quit()}
}
