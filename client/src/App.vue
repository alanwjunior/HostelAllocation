<template>
  <div id="app">
    <spinner />
    <router-view/>
  </div>
</template>

<script>
import Spinner from './components/Spinner.vue'
export default {
  components: {
    Spinner: Spinner
  }
}
</script>

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
