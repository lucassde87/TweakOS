@echo off

title BCD Timers - @ovymgmt
echo Applying correct BCD Timers configuration
bcdedit -deletevalue useplatformtick
bcdedit -deletevalue useplatformclock
bcdedit -set disabledynamictick true
timeout -t 3
exit
