module cloudsql-proxy-cs

go 1.12

replace github.com/expert1-pty-ltd/cloudsql-proxy => ./

require (
	bazil.org/fuse v0.0.0-20200419173433-3ba628eaf417
	cloud.google.com/go v0.56.0
	github.com/GoogleCloudPlatform/cloudsql-proxy v1.16.0
	github.com/expert1-pty-ltd/cloudsql-proxy v0.0.0-00010101000000-000000000000
	github.com/go-sql-driver/mysql v1.5.0
	github.com/lib/pq v1.3.0
	golang.org/x/crypto v0.0.0-20200420201142-3c4aac89819a
	golang.org/x/net v0.0.0-20200324143707-d3edc9973b7e
	golang.org/x/oauth2 v0.0.0-20200107190931-bf48bf16ab8d
	golang.org/x/sys v0.0.0-20200420163511-1957bb5e6d1f // indirect
	google.golang.org/api v0.21.0
	google.golang.org/grpc v1.29.1
)
