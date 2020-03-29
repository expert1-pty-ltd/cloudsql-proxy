set CGO_ENABLED=1 
set GOOS=linux
go build -buildmode=c-shared -o cloud_sql_proxy.so
pause