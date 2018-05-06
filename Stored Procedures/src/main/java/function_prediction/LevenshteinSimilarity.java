package function_prediction;

import org.neo4j.graphdb.GraphDatabaseService;
import org.neo4j.logging.Log;
import org.neo4j.procedure.*;

import java.util.ArrayList;
import java.util.List;
import java.util.stream.Stream;

public class LevenshteinSimilarity {

    // This field declares that we need a GraphDatabaseService
    // as context when any procedure in this class is invoked
    @Context
    public GraphDatabaseService db;

    // This gives us a log instance that outputs messages to the
    // standard log, normally found under `data/log/console.log`
    @Context
    public Log log;

    /**
     * This procedure is used to calculate the Levenshtein string distance between the new sequence and the consensus sequence of the passed node.
     * Step 1 - Generate k-mers of length node consensus. In the case that new sequence is shorter than consensus, find a piece of the consensus that matches new sequence
     * Step 2 - Find the region which matches the consensus in the new sequence or vice-versa
     * Step 3 - Return the highest match
     *
     * @param newSequence - The full sequence that requires classification
     * @param cluster - The cluster name
     * @param consensus - The consensus sequence of the current cluster being analyzed
     * @param gaps - The number of parsedGaps that are found in the consensus sequence
     * @param threshold - The value that the levenshtein similarity has to exceed for further analysis
     */

    @Procedure(value = "functionPrediction.levenshtein", mode= Mode.READ)
    @Description("This procedure is used to calculate the Levenshtein string distance between the new sequence and the consensus sequence of the passed node")
    public Stream<Match> shortlist(@Name("newSequence") String newSequence, @Name("cluster") String cluster, @Name("consensus") String consensus, @Name("gaps") String gaps, @Name("threshold") String threshold ){

        // Variables that will hold the Strings to compare
        String source;
        List<String> target;
        int maxLength;

        // Variables that will hold parsed values of the parameters
        int parsedGaps = Integer.parseInt(gaps);
        int parsedThreshold = Integer.parseInt(threshold);

        // Variables that will hold the results for the levenshtein similarity procedure
        double levenshteinSimilarity = 0;
        int regionStart = 0;
        int regionLength = 0;
        boolean reverseComparison = false;

        // Determine which of the new sequence and consensus sequence is the longest
        if(newSequence.length() >= consensus.length()){     // Normal comparison
            source = consensus;
            target = GenerateKmers(newSequence, consensus.length());
            maxLength = consensus.length();
        }
        else{       // Reverse comparison
            source = newSequence;
            target = GenerateKmers(consensus, newSequence.length());
            maxLength = newSequence.length();
            reverseComparison = true;
        }
        log.debug(cluster);

        // Iterate over all the k-mers and find the most similar region
        for(String kmer: target){

            // If this is a reverse comparison, the number of gaps need to be calculated each time
            if(reverseComparison){
                parsedGaps = NumberOfGaps(kmer);

                // Check if the current k-mer contains a lot of k-mers
                if(parsedGaps  >= (kmer.length() / 2)){
                    break;
                }
            }

            // Calculate the levenshtein string similarity
            double similarity = levenshteinDistance (source, kmer);
            double percentage = ((maxLength - similarity) / maxLength) * 100;

            // Check if the current similarity is greater than the highest similarity
            if(percentage >= levenshteinSimilarity){
                levenshteinSimilarity = percentage;
                regionStart = target.indexOf(kmer);
            }
        }

        // Check if the highest similarity exceeds the threshold
        if(levenshteinSimilarity >= parsedThreshold){
            regionLength = maxLength;
            return Stream.of(new Match(Math.round(levenshteinSimilarity), reverseComparison, (long)regionStart, (long)regionLength));
        }
        else{
            return Stream.empty();
        }
    }

    /**
     * A method that will calculate the levenshtein distance between two strings
     */
    public int levenshteinDistance (CharSequence lhs, CharSequence rhs) {
        int len0 = lhs.length() + 1;
        int len1 = rhs.length() + 1;

        // the array of distances
        int[] cost = new int[len0];
        int[] newcost = new int[len0];

        // initial cost of skipping prefix in String s0
        for (int i = 0; i < len0; i++) cost[i] = i;

        // dynamically computing the array of distances

        // transformation cost for each letter in s1
        for (int j = 1; j < len1; j++) {
            // initial cost of skipping prefix in String s1
            newcost[0] = j;

            // transformation cost for each letter in s0
            for(int i = 1; i < len0; i++) {
                // matching current letters in both strings
                int match = (((lhs.charAt(i - 1) == rhs.charAt(j - 1)) || ((lhs.charAt(i - 1) == '-') || (rhs.charAt(j - 1) == '-'))))? 0 : 1;

                // computing cost for each transformation
                int cost_replace = cost[i - 1] + match;
                int cost_insert  = cost[i] + 1;
                int cost_delete  = newcost[i - 1] + 1;

                // keep minimum cost
                newcost[i] = Math.min(Math.min(cost_insert, cost_delete), cost_replace);
            }

            // swap cost/newcost arrays
            int[] swap = cost; cost = newcost; newcost = swap;
        }

        // the distance is the cost for transforming all letters in both strings
        return cost[len0 - 1];
    }

    /**
     * A method that will generate a list of k-mers for the passed sequence of the passed length
     */
    public List<String> GenerateKmers(String sequence, int length){

        // A list that will hold all k-mers
        List<String> kmers = new ArrayList<String>();

        // Create the k-mers
        for(int i = 0; i <= (sequence.length() - length); i++){
            kmers.add(sequence.substring(i, ((i + length) - 1)));
        }

        // Return the k-mers
        return kmers;
    }

    /**
     * A method that will generate the number of gap characters in a consensus sequence
     */
    public int NumberOfGaps(String sequence){
        int gaps = 0;

        // Iterate over the whole sequence
        for(int i = 0; i < sequence.length(); i++){
            if(sequence.charAt(i) == '-'){
                gaps++;
            }
        }
        return gaps;
    }

    /**
     * This is the output record for our levenshtein procedure. All procedures
     * that return results return them as a Stream of Records, where the
     * records are defined like this one - customized to fit what the procedure
     * is returning.
     */
    public static class Match{
        // Properties
        public long levenshteinSimilarity;
        public boolean reverseComparison;
        public long regionStart;
        public long regionLength;

        // Constructor
        public Match(long levenshteinSimilarity, boolean reverseComparison, long regionStart, long regionLength){
            this.levenshteinSimilarity = levenshteinSimilarity;
            this.reverseComparison = reverseComparison;
            this.regionStart = regionStart;
            this.regionLength = regionLength;
        }
    }
}
