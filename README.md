# Static Content Provider (CDN Origin / CDN)

A project consisting of an ASP.NET Core project that has an assigned role of being a static content provider.\
Could easily be used in regards to CDN as a CDN origin for Cloudflare, AWS Cloudfront, Azure CDN, Cloud CDN and many more.

## Docker Setup

### Environment variables

`CACHECONTROL_MAXAGE` (double, default value is 12)\
`CACHECONTROL_MAXAGE_TIMEUNIT` (enum, default value is Hours)\
`CACHECONTROL_SHAREDMAXAGE` (double, default value is 168)\
`CACHECONTROL_SHAREDMAXAGE_TIMEUNIT` (enum, default value is Hours)\
`CDNROOT_DEFAULTFILES` (semi-colon-delimited string, default value is default.htm;default.html;index.htm;index.html)\
`CDNROOT` (filePath to the static files that need to be published, default value is /cdnroot)\
`ETAG_BYTESTOREAD` (int, default value is 2147483647 (strong validation))

For `CACHECONTROL_MAXAGE_TIMEUNIT` and `CACHECONTROL_SHAREDMAXAGE_TIMEUNIT` the allowed values are:

+ Days
+ Hours
+ Minutes
+ Seconds
+ Milliseconds
+ Tics

`CDNROOT_DEFAULTFILES` is used to activate the search for a default document, that will be served as default content.

For large files `ETAG_BYTESTOREAD` could be set to reduce how many bytes is being read per file. 

Do note, that if a file is read in its entirely, a strong ETag header value is generated; otherwise a weak ETag header value is generated.


### Embed files in image

```
FROM codebeltnet/web-cdn-origin:1.1.4

WORKDIR /cdnroot
ADD cdnroot .

WORKDIR /app
```
`docker build -t yourAmazingImage:SomeTag -f Dockerfile .`\
`docker run --name cdn-origin -d -p 8000:80 yourAmazingImage:SomeTag`

### Mount volume to image

`docker run --name cdn-origin -d -p 8000:80 -v some-path-with-static-files:/cdnroot codebeltnet/web-cdn-origin:1.1.4`

## Kubernetes Setup

### Running in Docker Desktop with mounted volume

```
apiVersion: apps/v1
kind: Deployment
metadata:
  name: codebelt-net
  labels:
    app: codebelt-net
spec:
  replicas: 1
  selector:
    matchLabels:
      app: codebelt-net
  template:
    metadata:
      labels:
        app: codebelt-net
    spec:
      restartPolicy: Always
      containers:
      - name: codebelt-net
        image: codebeltnet/web-cdn-origin:1.1.4
        env:
          - name: CACHECONTROL_SHAREDMAXAGE
            value: "24"
          - name: ETAG_BYTESTOREAD
            value: "512"
        ports:
        - containerPort: 80
        volumeMounts:
        - name: staticfiles
          mountPath: /cdnroot
      volumes:
        - name: staticfiles
          hostPath:
            type: Directory
            path: /run/desktop/mnt/host/c/codebelt-net/cdnroot
```
Code with passion; love your code; deliver with pride 👨‍💻️🔥❤️🚀🤘