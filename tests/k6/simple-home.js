import http from 'k6/http';
import { sleep, check } from 'k6';

export const options = {
  iterations: 1000,
  vus: 100
};

// The default exported function is gonna be picked up by k6 as the entry point for the test script. It will be executed repeatedly in "iterations" for the whole duration of the test.
export default function () {
  // Make a GET request to the target URL
  const res = http.get('https://sagra.edoardomacri.it');

  check(res, {
    'is status 200': (r) => r.status === 200,
  });
}