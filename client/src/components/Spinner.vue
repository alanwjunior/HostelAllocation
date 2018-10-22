<template>
  <div class="work-spinner" v-if="requestCounter > 0">
    <div id="loaderDiv"><img class="ajax-loader" src="../assets/spinner.gif" /></div>
  </div>
</template>

<script>
import axios from 'axios'
export default {
  data () {
    return {
      requestCounter: 0
    }
  },
  created () {
    axios.interceptors.request.use(
      config => {
        this.requestCounter++
        return config
      },
      error => Promise.reject(error))
    axios.interceptors.response.use(
      response => {
        this.requestCounter--
        return response
      },
      error => {
        this.requestCounter--
        return Promise.reject(error)
      })
  }
}
</script>

<style scoped>
#loaderDiv {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 1100;
  background-color: white;
  opacity: .6;
}

.ajax-loader {
  width: 350px;
  position: absolute;
  left: 50%;
  top: 50%;
  margin-left: -180px; /* -1 * image width / 2 */
  margin-top: -140px; /* -1 * image height / 2 */
  display: block;
}

.work-spinner {
  position: fixed;
  z-index: 1100;
  left: 45%;
  top: 65px;
}

</style>
