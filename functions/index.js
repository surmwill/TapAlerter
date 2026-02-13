/* eslint-disable */

const {setGlobalOptions} = require("firebase-functions");
const {onRequest} = require("firebase-functions/https");
const { onDocumentUpdated } = require("firebase-functions/v2/firestore");
const { getFirestore } = require("firebase-admin/firestore");
const { initializeApp } = require("firebase-admin/app");
const { onSchedule } = require("firebase-functions/v2/scheduler");
const admin = require("firebase-admin");
const config = require("./config");
const logger = require("firebase-functions/logger");

// For cost control, you can set the maximum number of containers that can be
// running at the same time. This helps mitigate the impact of unexpected
// traffic spikes by instead downgrading performance. This limit is a
// per-function limit. You can override the limit for each function using the
// `maxInstances` option in the function's options, e.g.
// `onRequest({ maxInstances: 5 }, (req, res) => { ... })`.
// NOTE: setGlobalOptions does not apply to functions using the v1 API. V1
// functions should each use functions.runWith({ maxInstances: 10 }) instead.
// In the v1 API, each function can only serve one request per container, so
// this will be the maximum concurrent request count.
setGlobalOptions({ maxInstances: 1 });

// Initialize the Admin SDK once at the top
initializeApp();
const db = getFirestore();

const userDocRef = db.collection("Users").doc(config.USER_DOC_GUID);

exports.setUserDirtyScheduled = onSchedule({
    schedule: "0 20 * * *",
    timeZone: "America/New_York", // Set this to your local timezone
}, async (event) => {
    console.log('ran');
    await userDocRef.update({ Dirty: true });
});

/*
// http://127.0.0.1:5001/tapalerter/us-central1/setUserDirtyManual
exports.setUserDirtyManual = onRequest(async (req, res) => {
    await userDocRef.update({ Dirty: true });
    res.status(200).send();
});
*/

exports.userTrigger = onDocumentUpdated(`Users/${config.USER_DOC_GUID}`, async (event) => {
    const user = event.data.after.data();
    if (!user.Dirty)
    {
        return;
    }

    const response = await fetch(`https://api.open-meteo.com/v1/forecast?latitude=45.5019&longitude=-73.561668&daily=temperature_2m_min&timezone=auto`);
    if (!response.ok) {
        console.log(`Error fetching weather. Status: ${response.status}`);
        return;
    }

    const data = await response.json();
    const tomorrowsDate = data.daily.time[1];
    const tomorrowsLow = data.daily.temperature_2m_min[1];
    
    await userDocRef.update({
        TomorrowsLow: tomorrowsLow,
        TomorrowsDate: tomorrowsDate,
        Dirty: false
    });

    console.log(tomorrowsDate, tomorrowsLow);

    const isEmulator = process.env.FUNCTIONS_EMULATOR === 'true';
    if (!user.Token || isEmulator)
    {
        return;
    }
    
    const message = {
        notification: {
            title: "Freezing Alert!",
            body: `⚠️⚠️⚠️ Tomorrow's low is ${tomorrowsLow}°C. Turn on the taps!`,
        },
        token: user.Token,
        android: {
            notification: {
                icon: 'icon_0',
                image: 'icon_1',
            }
        },
    };

    try {
        await admin.messaging().send(message);
        console.log("Notification sent successfully");
    } catch (error) {
        console.error("Error sending notification:", error);
    }
});
