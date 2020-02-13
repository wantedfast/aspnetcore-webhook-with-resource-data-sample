rem leave off the -subdomain=qna if you don't own qna.ngrok.io
ngrok http 5000 -host-header=localhost:5000 -subdomain=qna
