<template>
  <div id="app">
    <router-view/>
  </div>
</template>

<style>
#app {
  background-image: url("./assets/background.jpg");
  height: 100%;
  background-position: center;
  background-repeat: no-repeat;
  background-size: cover;
}

* {
  margin: 0px;
  padding: 0px;
}

h1 {
  font-family: 'Source Sans Pro', sans-serif;
}

HTML, body {
  height: 100%;
}
</style>
