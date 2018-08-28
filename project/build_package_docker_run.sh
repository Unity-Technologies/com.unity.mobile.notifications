set -xeuo pipefail

docker run \
	--rm \
	-v "$PWD":/package \
	buildpackagedocker.image \
    sh -c "$@"
