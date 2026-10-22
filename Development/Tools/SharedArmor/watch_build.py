import sys,time,json,psutil
from pathlib import Path
pid=int(sys.argv[1]);low=999;peak=0;reason=None
while psutil.pid_exists(pid):
 try:
  p=psutil.Process(pid);m=psutil.virtual_memory();free=m.available/2**30;low=min(low,free);peak=max(peak,p.memory_info().rss/2**30)
  if free<1.25:
   reason='Stopped QA build before physical memory exhaustion'
   for c in p.children(recursive=True):
    try:c.terminate()
    except psutil.Error:pass
   p.terminate();break
 except psutil.Error:break
 time.sleep(1)
Path('D:/Unity/AshfallArtValidation/shared-armor/build-resource-report.json').write_text(json.dumps(dict(min_free_ram_gb=round(low,2),peak_build_rss_gb=round(peak,2),abort=reason),indent=2))
