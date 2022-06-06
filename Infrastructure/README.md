
# Deployment
```cdktf deploy --auto-approve```

# Deploy
Open the first terminal, and run ```npm run watch``` to complie TypeScript.

Open the second terminal,
1. Run ```cdktf get```
2. Run ```cdktf deploy --auto-approve```

Get output
```cdktf output```

Save output to json
```cdktf output --outputs-file-include-sensitive-outputs --outputs-file secrets.json```