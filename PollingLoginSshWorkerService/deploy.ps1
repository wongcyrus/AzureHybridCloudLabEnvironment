sc.exe create "PollingLoginSshWorkerService" start=auto binpath="PollingLoginSshWorkerService.exe"
sc.exe failure "PollingLoginSshWorkerService" reset=0 actions=restart/60000/restart/60000/restart/60000
sc.exe query "PollingLoginSshWorkerService"
sc.exe qfailure "PollingLoginSshWorkerService"
